import { useState } from 'react'
import { IdentityPage } from './pages/IdentityPage'
import type { AuthResponse } from './api'

const IDENTITY_KEY = 'book-illustrator:identity'

function loadIdentity(): AuthResponse | null {
  const raw = localStorage.getItem(IDENTITY_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as AuthResponse
  } catch {
    return null
  }
}

function saveIdentity(identity: AuthResponse) {
  localStorage.setItem(IDENTITY_KEY, JSON.stringify(identity))
}

function App() {
  const [identity, setIdentity] = useState<AuthResponse | null>(loadIdentity)

  function handleAuthenticated(next: AuthResponse) {
    saveIdentity(next)
    setIdentity(next)
  }

  if (!identity) {
    return <IdentityPage onAuthenticated={handleAuthenticated} />
  }

  return (
    <div className="p-6 text-sm text-slate-600">
      Đã đăng nhập với tên {identity.name} ({identity.email})
    </div>
  )
}

export default App
