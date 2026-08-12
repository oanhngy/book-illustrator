import { useEffect, useState } from 'react'
import { ApiError, getProject, type ProjectDetail } from '../api'
import { Stepper } from '../components/Stepper'

interface ProjectDetailPageProps {
  userEmail: string
  projectId: string
  onBack: () => void
}

export function ProjectDetailPage({ userEmail, projectId, onBack }: ProjectDetailPageProps) {
  const [project, setProject] = useState<ProjectDetail | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setProject(null)
    setError(null)
    getProject(userEmail, projectId)
      .then((result) => {
        if (!cancelled) setProject(result)
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Không thể kết nối tới server.')
        }
      })
    return () => {
      cancelled = true
    }
  }, [userEmail, projectId])

  return (
    <div className="mx-auto max-w-2xl p-6">
      <button
        type="button"
        onClick={onBack}
        className="mb-4 text-sm text-slate-500 hover:text-slate-700"
      >
        ← Quay lại danh sách
      </button>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {!error && project === null && (
        <p className="text-sm text-slate-500">Đang tải dự án...</p>
      )}

      {!error && project !== null && (
        <>
          <h1 className="mb-4 text-lg font-semibold text-slate-900">{project.title}</h1>

          <div className="mb-6 rounded border border-slate-200 bg-white p-4">
            <Stepper completedSteps={project.completedSteps} runningStep={project.runningStep} />
          </div>

          <div className="mb-6 rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-2 text-sm font-medium text-slate-600">Nội dung sách</h2>
            <p className="max-h-64 overflow-y-auto whitespace-pre-wrap text-sm text-slate-700">
              {project.bookText}
            </p>
          </div>

          {project.completedSteps === 0 && (
            <p className="rounded border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
              Chưa có kết quả nào. Các bước sẽ hiện ở đây khi pipeline chạy.
            </p>
          )}
        </>
      )}
    </div>
  )
}
