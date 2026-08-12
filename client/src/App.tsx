import { useState } from 'react'
import { IdentityPage } from './pages/IdentityPage'
import { ProjectListPage } from './pages/ProjectListPage'
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

  return <ProjectListPage userEmail={identity.email} />
}

export default App
