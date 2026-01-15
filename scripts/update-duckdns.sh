#!/bin/bash
# DuckDNS Update Script
# Updates your DuckDNS domain with your current IP

DOMAIN="Granuch"  # Change to your DuckDNS subdomain (without .duckdns.org)
TOKEN="
f3debb26-cb1e-486d-a70a-66f4012b4330"  # Get this from duckdns.org

echo "Updating DuckDNS..."
curl -s "https://www.duckdns.org/update?domains=$DOMAIN&token=$TOKEN&ip="

echo ""
echo "Your site is available at: http://$DOMAIN.duckdns.org"
