#!/bin/bash
set -e
echo "=== backend ==="
dotnet test
echo ""
echo "=== frontend ==="
(cd client && npm test)
