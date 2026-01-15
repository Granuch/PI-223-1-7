#!/bin/bash

# SSL Certificate Setup Script using Certbot
# Run this script on your VPS after setting up your domain

DOMAIN="yourdomain.com"  # Change this to your actual domain

echo "=== SSL Certificate Setup for $DOMAIN ==="

# Install Certbot
apt update
apt install -y certbot

# Stop nginx temporarily
docker compose stop nginx

# Get certificate
certbot certonly --standalone -d $DOMAIN -d www.$DOMAIN --non-interactive --agree-tos --email your@email.com

# Create ssl directory
mkdir -p nginx/ssl

# Copy certificates
cp /etc/letsencrypt/live/$DOMAIN/fullchain.pem nginx/ssl/
cp /etc/letsencrypt/live/$DOMAIN/privkey.pem nginx/ssl/

# Update nginx.conf to use SSL (uncomment SSL lines)
# Then restart nginx
docker compose up -d nginx

echo "=== SSL Certificate installed! ==="
echo "Your site is now available at https://$DOMAIN"

# Setup auto-renewal
echo "0 0 1 * * certbot renew --pre-hook 'docker compose stop nginx' --post-hook 'docker compose start nginx'" | crontab -
