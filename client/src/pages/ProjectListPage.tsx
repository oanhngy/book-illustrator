import { useEffect, useState } from 'react'
import { ApiError, listProjects, type ProjectSummary } from '../api'

interface ProjectListPageProps {
  userEmail: string
}

export function ProjectListPage({ userEmail }: ProjectListPageProps) {
  const [projects, setProjects] = useState<ProjectSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setProjects(null)
    setError(null)
    listProjects(userEmail)
      .then((result) => {
        if (!cancelled) setProjects(result)
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Không thể kết nối tới server.')
        }
      })
    return () => {
      cancelled = true
    }
  }, [userEmail])

  return (
    <div className="mx-auto max-w-2xl p-6">
      <h1 className="mb-4 text-lg font-semibold text-slate-900">Dự án của bạn</h1>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {!error && projects === null && (
        <p className="text-sm text-slate-500">Đang tải danh sách dự án...</p>
      )}

      {!error && projects !== null && projects.length === 0 && (
        <p className="rounded border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
          Chưa có dự án nào.
        </p>
      )}

      {!error && projects !== null && projects.length > 0 && (
        <ul className="divide-y divide-slate-200 rounded border border-slate-200">
          {projects.map((project) => (
            <li key={project.id} className="p-4">
              <div className="font-medium text-slate-900">{project.title}</div>
              <div className="text-sm text-slate-500">
                {project.completedSteps}/5 bước hoàn thành ·{' '}
                {new Date(project.createdAt).toLocaleDateString('vi-VN')}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
