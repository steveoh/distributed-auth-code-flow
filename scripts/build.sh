set -e
cd "$(dirname "$0")/.."

docker build . -t auth-ticket
