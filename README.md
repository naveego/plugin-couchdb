# plugin-couchdb

To Rebuild the publisher contract:
- Restore Nuget packages
- Download the new `publisher.proto` file from dataflow-contacts
- Run the below command in the `plugin-couchdb/PluginCouchDB` directory

Windows
```
%UserProfile%\.nuget\packages\Grpc.Tools\2.24.0\tools\windows_x86\protoc.exe -I../ --csharp_out ./Publisher --grpc_out ./Publisher ../publisher.proto --plugin=protoc-gen-grpc=C:\Users\Laptop\.nuget\packages\grpc.tools\2.24.0\tools\windows_x86\grpc_csharp_plugin.exe```
```

Linux
```
~/.nuget/packages/grpc.tools/2.24.0/tools/linux_x64/protoc -I../ --csharp_out ./Publisher --grpc_out ./Publisher ../publisher.proto --plugin=protoc-gen-grpc=$HOME/.nuget/packages/grpc.tools/2.24.0/tools/linux_x64/grpc_csharp_plugin
```