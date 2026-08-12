#!/bin/bash
set -e

if [ ! -f .env ]; then
  echo "Missing .env — copy .env.example and add your Gemini API key."
  exit 1
fi
export $(grep -v '^#' .env | grep -v '^$' | xargs)

(cd server && dotnet run --urls http://localhost:5050) &
SERVER_PID=$!
(cd client && npm run dev) &
CLIENT_PID=$!
trap "kill $SERVER_PID $CLIENT_PID 2>/dev/null" EXIT

echo ""
echo "  API      http://localhost:5050"
echo "  Frontend http://localhost:5173"
echo ""
wait
