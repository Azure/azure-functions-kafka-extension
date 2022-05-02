import logging
import typing
from azure.functions import Out, HttpRequest, HttpResponse
import json

def main(req: HttpRequest, outputMessage: Out[str] ) -> HttpResponse:
    outputMessage.set(['one', 'two'])
    return 'OK'