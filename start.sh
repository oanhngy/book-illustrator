#!/bin/bash
set -e

if [ ! -f .env ]; then
  echo "Missing .env — copy .env.example and add your Gemini API key."
  exit 1
fi
export $(grep -v '^#' .env | grep -v '^$' | xargs)

# free the ports in case a previous run left something behind
lsof -ti:5050 | xargs kill -9 2>/dev/null || true
lsof -ti:5173 | xargs kill -9 2>/dev/null || true

(cd server && dotnet run --no-launch-profile --urls http://localhost:5050) &
SERVER_PID=$!
(cd client && npm run dev) &
CLIENT_PID=$!

cleanup() {
  kill $SERVER_PID $CLIENT_PID 2>/dev/null || true
  lsof -ti:5050 | xargs kill -9 2>/dev/null || true
  lsof -ti:5173 | xargs kill -9 2>/dev/null || true
}
trap cleanup EXIT INT TERM

echo ""
echo "  API      http://localhost:5050"
echo "  Frontend http://localhost:5173"
echo ""
wait
