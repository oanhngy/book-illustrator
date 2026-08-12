const STEP_LABELS = ['Style', 'Characters', 'Portraits', 'Chapters', 'Illustrations']

interface StepperProps {
  completedSteps: number
  runningStep: number | null
}

type StepStatus = 'done' | 'pending'
// TODO: 'current' once the run endpoint sets runningStep — steps run one at a time,
// so at most one step can ever be 'current'.

function stepStatus(index: number, completedSteps: number): StepStatus {
  return index < completedSteps ? 'done' : 'pending'
}

export function Stepper({ completedSteps, runningStep }: StepperProps) {
  void runningStep

  return (
    <ol className="flex w-full items-start">
      {STEP_LABELS.map((label, index) => {
        const status = stepStatus(index, completedSteps)
        const isLast = index === STEP_LABELS.length - 1

        return (
          <li key={label} className="flex flex-1 items-center">
            <div className="flex flex-col items-center gap-1">
              <div
                className={
                  'flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-medium ' +
                  (status === 'done'
                    ? 'bg-slate-900 text-white'
                    : 'border border-slate-300 bg-white text-slate-400')
                }
              >
                {status === 'done' ? '✓' : index + 1}
              </div>
              <span
                className={
                  'text-xs ' + (status === 'done' ? 'text-slate-900' : 'text-slate-400')
                }
              >
                {label}
              </span>
            </div>
            {!isLast && (
              <div
                className={
                  'mx-2 h-px flex-1 ' + (status === 'done' ? 'bg-slate-900' : 'bg-slate-200')
                }
              />
            )}
          </li>
        )
      })}
    </ol>
  )
}
