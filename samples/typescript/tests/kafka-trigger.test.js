const assert = require("assert");
const { execFileSync } = require("child_process");
const fs = require("fs");
const os = require("os");
const path = require("path");

const sampleRoot = path.resolve(__dirname, "..");
const samples = [
    { name: "KafkaTrigger" },
    { name: "KafkaTriggerRetry", retry: true },
    { name: "KafkaTriggerRetryExponential", retry: true },
    { name: "KafkaTriggerWithHeaders", headers: true },
    { name: "KafkaTriggerManyWithHeaders", headers: true, batch: true }
];
const outputDirectory = fs.mkdtempSync(path.join(os.tmpdir(), "kafka-trigger-tests-"));

async function run() {
    // Compile only these triggers. Other samples are not part of this test.
    execFileSync(process.execPath, [
        require.resolve("typescript/bin/tsc"),
        ...samples.map(sample => path.join(sampleRoot, sample.name, "index.ts")),
        "--module", "commonjs",
        "--target", "es6",
        "--rootDir", sampleRoot,
        "--outDir", outputDirectory,
        "--noEmitOnError"
    ], { cwd: sampleRoot, stdio: "inherit" });

    const code = "globalThis.__kafkaTriggerCodeExecuted = true";
    const payload = {
        registertime: 1234567890,
        userid: code,
        regionid: "region_1",
        gender: "FEMALE"
    };
    const event = {
        Offset: 42,
        Partition: 1,
        Topic: "topic",
        Timestamp: "2026-09-17T00:00:00Z",
        Value: JSON.stringify({ payload }),
        Headers: [{ Key: "language", Value: Buffer.from("typescript").toString("base64") }]
    };

    for (const sample of samples) {
        const trigger = require(path.join(outputDirectory, sample.name, "index.js")).default;
        const logs = [];
        const context = { log: (...args) => logs.push(args) };
        const input = value => sample.batch ? [value] : value;
        globalThis.__kafkaTriggerCodeExecuted = false;

        const invocation = trigger(context, input(JSON.stringify(event)));
        if (sample.retry) {
            await assert.rejects(invocation, { message: "Unhandled Error" });
        } else {
            await invocation;
        }

        const expectedLogs = [
            ["Event Offset: 42"],
            ["Event Partition: 1"],
            ["Event Topic: topic"],
            ["Event Timestamp: " + event.Timestamp],
            ["Event Value (as string): " + event.Value]
        ];
        if (sample.headers) {
            expectedLogs.push(["Event Headers: "], ["Key: ", "language", "Value: ", "typescript"]);
        } else {
            expectedLogs.push(
                ["Event Value Object: "],
                ["   Value.registertime: ", "1234567890"],
                ["   Value.userid: ", code],
                ["   Value.regionid: ", "region_1"],
                ["   Value.gender: ", "FEMALE"]
            );
        }
        assert.deepStrictEqual(logs, expectedLogs, sample.name);
        assert.strictEqual(globalThis.__kafkaTriggerCodeExecuted, false, sample.name);

        if (sample.batch) {
            logs.length = 0;
            await trigger(context, [JSON.stringify(event), JSON.stringify(event)]);
            assert.deepStrictEqual(logs, [...expectedLogs, ...expectedLogs], sample.name);
        }

        logs.length = 0;
        await assert.rejects(trigger(context, input("{")), SyntaxError);
        assert.deepStrictEqual(logs, [], sample.name);

        const executable = `(${code}, ${JSON.stringify(event)})`;
        await assert.rejects(trigger(context, input(executable)), SyntaxError);
        assert.strictEqual(globalThis.__kafkaTriggerCodeExecuted, false, sample.name);
        assert.deepStrictEqual(logs, [], sample.name);

        if (sample.batch) {
            // A second JSON layer must not turn a string into executable code.
            await assert.rejects(trigger(context, [JSON.stringify(executable)]));
            assert.strictEqual(globalThis.__kafkaTriggerCodeExecuted, false, sample.name);
        }

        console.log(`PASS ${sample.name}`);
    }
}

run().catch(error => {
    console.error(error);
    process.exitCode = 1;
}).finally(() => {
    delete globalThis.__kafkaTriggerCodeExecuted;
    fs.rmSync(outputDirectory, { recursive: true, force: true });
});
