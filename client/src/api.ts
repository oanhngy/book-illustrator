// Thin fetch wrapper over the real backend endpoints.
// Field names match the server's camelCase JSON (System.Text.Json web defaults).

export type AuthResponse = {
  userId: string
  name: string
  email: string
}

export type ProjectSummary = {
  id: string
  title: string
  createdAt: string
  completedSteps: number
  runningStep: number | null
}

export type ProjectDetail = {
  id: string
  title: string
  createdAt: string
  bookText: string
  completedSteps: number
  runningStep: number | null
  runningSince: string | null
  lastError: string | null
  failedStep: number | null
  canForceRetry: boolean
  styleJson: string | null
  charactersJson: string | null
  chaptersJson: string | null
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`/api${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  })

  if (!res.ok) {
    const text = await res.text()
    throw new ApiError(res.status, text || res.statusText)
  }

  return res.json() as Promise<T>
}

export function auth(email: string, name: string): Promise<AuthResponse> {
  return request<AuthResponse>('/auth', {
    method: 'POST',
    body: JSON.stringify({ email, name }),
  })
}

export function listProjects(userEmail: string): Promise<ProjectSummary[]> {
  return request<ProjectSummary[]>('/projects', {
    headers: { 'X-User-Email': userEmail },
  })
}

export function createProject(
  userEmail: string,
  title: string,
  bookText: string,
): Promise<ProjectSummary> {
  return request<ProjectSummary>('/projects', {
    method: 'POST',
    headers: { 'X-User-Email': userEmail },
    body: JSON.stringify({ title, bookText }),
  })
}

export function getProject(userEmail: string, id: string): Promise<ProjectDetail> {
  return request<ProjectDetail>(`/projects/${id}`, {
    headers: { 'X-User-Email': userEmail },
  })
}
