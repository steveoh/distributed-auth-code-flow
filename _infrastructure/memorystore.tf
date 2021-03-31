resource "google_compute_network" "redis-network" {
  name = "redis-network"
}

resource "google_compute_global_address" "service_range" {
  name          = "address"
  purpose       = "VPC_PEERING"
  address_type  = "INTERNAL"
  prefix_length = 16
  network       = google_compute_network.redis-network.id
}

resource "google_service_networking_connection" "private_service_connection" {
  network                 = google_compute_network.redis-network.id
  service                 = "servicenetworking.googleapis.com"
  reserved_peering_ranges = [google_compute_global_address.service_range.name]
}

resource "google_redis_instance" "cache" {
  name           = "redis-identity-cache"
  tier           = "BASIC"
  memory_size_gb = 1

  region = var.region

  authorized_network = google_compute_network.redis-network.id
  connect_mode       = "PRIVATE_SERVICE_ACCESS"

  redis_version = "REDIS_5_0"
  display_name  = "Identity Ticket Cache"

  depends_on = [google_service_networking_connection.private_service_connection]
}

resource "google_vpc_access_connector" "serverless-connector" {
  name           = "redis-serverless-vpc"
  ip_cidr_range  = "10.8.0.0/28"
  region         = var.region
  network        = google_compute_network.redis-network.name
  min_throughput = 200
  max_throughput = 300
}

output "redis_host" {
  value = google_redis_instance.cache.host
}
output "redis_port" {
  value = google_redis_instance.cache.port
}
output "redis_current_location_id" {
  value = google_redis_instance.cache.current_location_id
}
