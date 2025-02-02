gRPC-сервер сгенерирован на основе `service.proto` с помощью команды
```python -m grpc_tools.protoc -I. --python_out=./CalendarsApi/ --grpc_python_out=./CalendarsApi/ ./Protos/calendars.proto```
в директории src