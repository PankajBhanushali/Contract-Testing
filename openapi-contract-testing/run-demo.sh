#!/bin/bash

# Quick Start Script for OpenAPI Contract Testing Demo

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║  OpenAPI Contract Testing Demo - Quick Start           ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

# Check if provider is already running
echo "[1/4] Checking for existing containers..."
if docker-compose ps | grep -q "provider-api"; then
    echo "✓ Provider already running"
else
    echo "[2/4] Starting Provider (Docker)..."
    docker-compose up -d
    echo "✓ Provider started"
    
    echo ""
    echo "[3/4] Waiting for provider to be healthy..."
    sleep 5
fi

echo ""
echo "[4/4] Running Consumer Tests..."
echo ""

# Run tests
cd consumer
npm install --silent
npm test

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║  Test Run Complete                                     ║"
echo "║                                                         ║"
echo "║  Provider: http://localhost:5001                       ║"
echo "║                                                         ║"
echo "║  Commands:                                              ║"
echo "║  - npm run test:v1   : SF 17.1 tests only              ║"
echo "║  - npm run test:v2   : SF 18.1 tests only              ║"
echo "║  - npm run test:watch: Watch mode                      ║"
echo "║                                                         ║"
echo "║  To stop provider: docker-compose down                 ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""
