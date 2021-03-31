application     = "plss"
environment     = "dev"
folder_id       = "941321792651"
region          = "us-central1"
services        = [
  "logging.googleapis.com", // logging
  "redis.googleapis.com", // caching
  "servicenetworking.googleapis.com", // redis network
  "vpcaccess.googleapis.com" // redis network access
]
project_labels  = {
  app           = "plss"
  contact       = "sgourley"
  dept          = "agr"
  elcid         = "itagrcgps"
  env           = "dev"
  security      = "0"
  supportcode   = "void"
}
