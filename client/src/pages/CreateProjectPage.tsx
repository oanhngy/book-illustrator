import { useState } from 'react'
import { ApiError, createProject, type ProjectSummary } from '../api'

interface CreateProjectPageProps {
  userEmail: string
  onCreated: (project: ProjectSummary) => void
  onCancel: () => void
}

export function CreateProjectPage({ userEmail, onCreated, onCancel }: CreateProjectPageProps) {
  const [title, setTitle] = useState('')
  const [bookText, setBookText] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const project = await createProject(userEmail, title.trim(), bookText.trim())
      onCreated(project)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Không thể kết nối tới server.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-lg font-semibold text-slate-900">Tạo dự án mới</h1>
        <button
          type="button"
          onClick={onCancel}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          Quay lại danh sách
        </button>
      </div>

      <form onSubmit={handleSubmit} className="rounded border border-slate-200 bg-white p-6">
        <label className="mb-3 block text-sm">
          <span className="mb-1 block text-slate-600">Tên dự án</span>
          <input
            type="text"
            required
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className="w-full rounded border border-slate-300 px-3 py-2 outline-none focus:border-slate-500"
          />
        </label>

        <label className="mb-4 block text-sm">
          <span className="mb-1 block text-slate-600">Nội dung sách</span>
          <textarea
            required
            rows={10}
            value={bookText}
            onChange={(e) => setBookText(e.target.value)}
            className="w-full resize-y rounded border border-slate-300 px-3 py-2 outline-none focus:border-slate-500"
          />
        </label>

        {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="rounded bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          {submitting ? 'Đang tạo...' : 'Tạo dự án'}
        </button>
      </form>
    </div>
  )
}
