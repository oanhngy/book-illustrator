import { useEffect, useState } from 'react'
import { ApiError, getProject, runStep, type ProjectDetail } from '../api'
import { STEP_LABELS, Stepper } from '../components/Stepper'

interface ProjectDetailPageProps {
  userEmail: string
  projectId: string
  onBack: () => void
}

export function ProjectDetailPage({ userEmail, projectId, onBack }: ProjectDetailPageProps) {
  const [project, setProject] = useState<ProjectDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [starting, setStarting] = useState(false)

  // Tải dữ liệu project lần đầu khi vào trang.
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

  // Việc 1: poll mỗi 2s trong lúc có bước đang chạy, dừng khi runningStep về null.
  useEffect(() => {
    if (project === null || project.runningStep === null) return

    const interval = setInterval(() => {
      getProject(userEmail, projectId)
        .then(setProject)
        .catch(() => {
          // Lỗi poll thoáng qua — lượt sau tự thử lại, không cần báo lỗi ra UI.
        })
    }, 2000)

    return () => clearInterval(interval)
  }, [userEmail, projectId, project?.runningStep])

  // Dùng chung cho: nút "chạy bước tiếp theo", nút "thử lại", nút "force-retry".
  async function handleRunStep(step: number) {
    setStarting(true)
    setActionError(null)
    try {
      await runStep(userEmail, projectId, step)
      const updated = await getProject(userEmail, projectId)
      setProject(updated)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Không thể kết nối tới server.')
    } finally {
      setStarting(false)
    }
  }

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

          {/*
            Việc 5: chiều cao tối thiểu cố định cho cả khối trạng thái — đây là khối đổi
            hình dạng nhiều nhất khi kết quả đến (loading 1 dòng <-> error card <-> nút <->
            rỗng khi xong cả 5 bước). Không có min-h thì "Nội dung sách" và lưới ảnh bên
            dưới sẽ nhảy lên/xuống mỗi lần poll thấy state đổi.
          */}
          <div className="mb-6 min-h-32">
            {/* Việc 2: loading nêu tên step, không phải spinner chung chung */}
            {project.runningStep !== null && (
              <p className="text-sm text-blue-700">
                Đang chạy: {STEP_LABELS[project.runningStep - 1]}...
                {project.canForceRetry && (
                  <button
                    type="button"
                    onClick={() => handleRunStep(project.runningStep!)}
                    disabled={starting}
                    className="ml-2 underline disabled:opacity-50"
                  >
                    Bước này có vẻ bị treo — thử chạy lại
                  </button>
                )}
              </p>
            )}

            {/* Việc 3: error per-step + nút retry */}
            {project.runningStep === null && project.lastError !== null && project.failedStep !== null && (
              <div className="rounded border border-red-200 bg-red-50 p-4">
                <p className="mb-2 text-sm text-red-700">
                  Bước {STEP_LABELS[project.failedStep - 1]} lỗi: {project.lastError}
                </p>
                <button
                  type="button"
                  onClick={() => handleRunStep(project.failedStep!)}
                  disabled={starting}
                  className="rounded bg-red-600 px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
                >
                  {starting ? 'Đang thử lại...' : 'Thử lại'}
                </button>
              </div>
            )}

            {/* Nút chạy bước tiếp theo — cần có để tự kích hoạt được polling/loading ở trên */}
            {project.runningStep === null && project.lastError === null && project.completedSteps < 5 && (
              <button
                type="button"
                onClick={() => handleRunStep(project.completedSteps + 1)}
                disabled={starting}
                className="rounded bg-slate-900 px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
              >
                {starting ? 'Đang bắt đầu...' : `Chạy bước: ${STEP_LABELS[project.completedSteps]}`}
              </button>
            )}

            {actionError && <p className="mt-2 text-sm text-red-600">{actionError}</p>}
          </div>

          <div className="mb-6 rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-2 text-sm font-medium text-slate-600">Nội dung sách</h2>
            <p className="max-h-64 overflow-y-auto whitespace-pre-wrap text-sm text-slate-700">
              {project.bookText}
            </p>
          </div>

          {project.completedSteps === 0 && project.runningStep === null && (
            <p className="rounded border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
              Chưa có kết quả nào. Bấm nút bên trên để bắt đầu.
            </p>
          )}

          {/* Việc 5: mỗi ô ảnh cỡ cố định — ảnh mới xuất hiện không làm ảnh cũ đổi kích thước */}
          {project.images.length > 0 && (
            <div className="grid grid-cols-3 gap-3">
              {project.images.map((img) => (
                <div key={img.id} className="aspect-square overflow-hidden rounded border border-slate-200">
                  <img src={img.url} alt="" className="h-full w-full object-cover" />
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  )
}
