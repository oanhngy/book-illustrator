import { useEffect, useState } from 'react'
import { ApiError, listProjects, type ProjectSummary } from '../api'

interface ProjectListPageProps {
  userEmail: string
  onCreateNew: () => void
  onSelectProject: (projectId: string) => void
}

export function ProjectListPage({ userEmail, onCreateNew, onSelectProject }: ProjectListPageProps) {
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
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-lg font-semibold text-slate-900">Dự án của bạn</h1>
        <button
          type="button"
          onClick={onCreateNew}
          className="rounded bg-slate-900 px-3 py-2 text-sm font-medium text-white"
        >
          Tạo dự án mới
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {!error && projects === null && (
        <p className="text-sm text-slate-500">Đang tải danh sách dự án...</p>
      )}

      {!error && projects !== null && projects.length === 0 && (
        <p className="rounded border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
          Chưa có dự án nào. Bấm "Tạo dự án mới" để bắt đầu.
        </p>
      )}

      {!error && projects !== null && projects.length > 0 && (
        <ul className="divide-y divide-slate-200 rounded border border-slate-200">
          {projects.map((project) => (
            <li key={project.id}>
              <button
                type="button"
                onClick={() => onSelectProject(project.id)}
                className="w-full p-4 text-left hover:bg-slate-50"
              >
                <div className="font-medium text-slate-900">{project.title}</div>
                <div className="text-sm text-slate-500">
                  {project.completedSteps}/5 bước hoàn thành ·{' '}
                  {new Date(project.createdAt).toLocaleDateString('vi-VN')}
                </div>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
