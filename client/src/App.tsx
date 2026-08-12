import { useState } from 'react'
import { IdentityPage } from './pages/IdentityPage'
import { ProjectListPage } from './pages/ProjectListPage'
import { CreateProjectPage } from './pages/CreateProjectPage'
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

type View = { name: 'list' } | { name: 'create' }

function App() {
  const [identity, setIdentity] = useState<AuthResponse | null>(loadIdentity)
  const [view, setView] = useState<View>({ name: 'list' })

  function handleAuthenticated(next: AuthResponse) {
    saveIdentity(next)
    setIdentity(next)
  }

  if (!identity) {
    return <IdentityPage onAuthenticated={handleAuthenticated} />
  }

  if (view.name === 'create') {
    return (
      <CreateProjectPage
        userEmail={identity.email}
        onCreated={() => setView({ name: 'list' })}
        onCancel={() => setView({ name: 'list' })}
      />
    )
  }

  return (
    <ProjectListPage
      userEmail={identity.email}
      onCreateNew={() => setView({ name: 'create' })}
    />
  )
}

export default App
