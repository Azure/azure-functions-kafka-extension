import azure.functions as func

def main(req: func.HttpRequest, out: func.Out[str]):
    event = req.get_body().decode('utf-8')
    out.set(event)
    return event
