#!/bin/bash
# One-time setup script for the Azure VM (Ubuntu 24.04).
# Run as root: sudo bash setup.sh
set -euo pipefail

echo "=== Installing Docker ==="
apt-get update
apt-get install -y ca-certificates curl git

install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
| tee /etc/apt/sources.list.d/docker.list > /dev/null

apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Allow the deploy user to run Docker without sudo.
usermod -aG docker "${SUDO_USER:-azureuser}"

echo "=== Cloning repo ==="
mkdir -p /opt/tenantportal
cd /opt/tenantportal
git clone https://github.com/Shubh1229/TenantPortal.git .

# Allow the deploy user to own the app directory.
chown -R "${SUDO_USER:-azureuser}:${SUDO_USER:-azureuser}" /opt/tenantportal

echo ""
echo "=== Setup complete. Next steps: ==="
echo "1. Copy .env.prod.template to /opt/tenantportal/.env.prod and fill in every value."
echo "2. In the Stripe Dashboard, register the webhook endpoint:"
echo "   URL: https://singhrentalhub.com/api/webhooks/stripe"
echo "   Events: payment_intent.succeeded, customer.subscription.*, invoice.payment_failed"
echo "   Then paste the whsec_... secret into .env.prod as STRIPE_WEBHOOK_SECRET."
echo "3. In Namecheap DNS, add an A record: @ -> <this VM's public IP>"
echo "   (also add A record: www -> same IP)"
echo "4. Open ports 80 and 443 in the Azure VM's Network Security Group."
echo "5. Run the first deploy:"
echo "   cd /opt/tenantportal"
echo "   docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build"
