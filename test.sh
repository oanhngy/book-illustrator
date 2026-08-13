#!/bin/bash
set -e
echo "=== backend ==="
dotnet test
# Frontend component tests (Block F) were cut for time — see PLAN.md's cut list
# and "one more day". Nothing to run here until that changes.
