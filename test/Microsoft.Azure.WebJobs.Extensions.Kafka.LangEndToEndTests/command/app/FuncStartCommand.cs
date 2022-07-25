using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app
{
    /* Shell Command responsible for creating running the docker container 
     * containing function app images for particular language.
    */
	public class FuncStartCommand : ShellCommand
	{
		public FuncStartCommand(BrokerType brokerType, Language language) 
		{
            cmd = buildFuncStartCmd(brokerType, language);
            Console.WriteLine(cmd);
        }

        private string buildFuncStartCmd(BrokerType brokerType, Language language)
        {
            // Using --script-root instead of cd
            // create function folder using the borker and language type - util function
            List<string> cmdList = new List<string>() { };
            var funcFloder = Utils.BuildFuncRootPath(brokerType, language);
            if (Language.JAVA == language)
            {
                cmdList.Add(Constants.MVN_CLN_PKG);
                cmdList.Add(Constants.SCRIPT_ROOT);
                cmdList.Add(funcFloder);
                cmdList.Add(Constants.CMD_AND);
                cmdList.Add(Constants.MVN_RUN_FUNC);
                cmdList.Add(Constants.SCRIPT_ROOT);
                cmdList.Add(funcFloder);
            }
            else 
            { 
                cmdList.Add(Constants.FUNC_EXT_INSTALL);
                cmdList.Add(Constants.SCRIPT_ROOT);
                cmdList.Add(funcFloder);
                cmdList.Add(Constants.CMD_AND);
                cmdList.Add(Constants.FUNC_START);
                cmdList.Add(Constants.SCRIPT_ROOT);
                cmdList.Add(funcFloder);
            }
            
            cmdList.Add(Constants.FUNC_PORT_FLAG);
            cmdList.Add($"{Constants.BrokerLanguagePortMapping[new Tuple<BrokerType, Language>(brokerType, language)]}");

            cmdList.Add($"> {brokerType}{Constants.LanguageRuntimeMapping[language]}startup.txt 2>&1");
            //cmdList.Add($">startup.txt 2>&1");
            
            return string.Join(Constants.SPACE_CHAR, cmdList);
        }

    }
}
