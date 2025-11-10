# Docker support

* Write a Dockerfile and .dockerignore to build the docker container.
* Write a docker-compose.yml that brings up azurite and azurite-ui together.
  * Set up the Account information for Azurite in such a way that .env can be used to alter.
* Write a target for `dotnet cake` that builds the docker container.
  * `dotnet cake --target=CI should also build the docker container.
* Update README.md with the information.
* Run `dotnet cake --target=CI` to verify the work.
* Run `docker compose up -d` to verify the work.
