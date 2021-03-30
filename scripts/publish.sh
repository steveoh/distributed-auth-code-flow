set -e

echo 'building'

./scripts/build.sh

cd "$(dirname "$0")/.."

docker tag auth-ticket gcr.io/ut-dts-agrc-plss-dev/auth-ticket

echo 'pushing to gcr.io'

docker push gcr.io/ut-dts-agrc-plss-dev/auth-ticket
