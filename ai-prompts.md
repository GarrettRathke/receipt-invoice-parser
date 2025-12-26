# AI Prompts

This is a high level overview of what prompts were used to create the project.

## Project Scaffolding
### Plan Phase 
__Prompt__: ```Create a HelloWorld monorepo configuration having a C# backend with .NET 10 and an Angular frontend. Also add Docker support for local development.```

__Planning Agent Response__: ```Further Considerations
Port configuration: Backend on 5000 (external) mapping to 8080 (internal .NET 10 default), frontend on 4200 - need other port mappings?
Development containers: Include full VS Code devcontainer setup with extensions, or prefer manual Docker Compose workflow?
Database integration: Plan for future database service in Docker Compose, or keep simple for HelloWorld scope?```

__Additional Prompt Instructions__:
1. The default port mappings are sufficient.
2. Use a Docker Compose workflow.
3. Keep it simple for the HelloWorld scope.


## Project Main Features