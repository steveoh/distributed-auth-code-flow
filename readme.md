# dotnet serverless auth

This demo uses the State of Utah Open Id Connect and an Authorization Code flow with PKCE for authentication.

Once authenticated, auth is swapped to a dotnet managed cookie and the authentication tickets are stored in a distributed redis cache along with the data protection key to decrypt the auth cookie. This allows any dotnet process with access to the distributed cache to authenticate clients which is great for serverless or load balanced scenarios as no auth information is stored in memory.

This demo is running in GCP with Cloud Run and a redis Memorystore.
