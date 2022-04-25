import logging

import azure.functions as func
import json

def main(req: func.HttpRequest, out: func.Out[str]) -> func.HttpResponse:
    message = req.params.get('message')
    kevent =  { "Offset":364,"Partition":0,"Topic":"kafkaeventhubtest1","Timestamp":"2022-04-09T03:20:06.591Z", "Value": message, "Headers": [{ "Key": "test", "Value": "python" }] }
    out.set(json.dumps(kevent))
    return 'OK'
