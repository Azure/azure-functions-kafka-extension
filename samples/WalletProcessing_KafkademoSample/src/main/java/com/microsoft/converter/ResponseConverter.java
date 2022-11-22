package com.microsoft.converter;

import com.fasterxml.jackson.core.JsonProcessingException;

import java.io.IOException;

public interface ResponseConverter<Request, Response> {
    Response convert(Request request) throws IOException;
}
