export const STEP_LABELS = ['Style', 'Characters', 'Portraits', 'Chapters', 'Illustrations']

interface StepperProps {
  completedSteps: number
  runningStep: number | null
}

type StepStatus = 'done' | 'current' | 'pending'

function stepStatus(index: number, completedSteps: number, runningStep: number | null): StepStatus {
  if(index<completedSteps) return 'done'
  if(runningStep !== null && index===runningStep-1) return 'current'
  return 'pending'
}

export function Stepper({ completedSteps, runningStep }: StepperProps) {
  return (
    <ol className="flex w-full items-start">
      {STEP_LABELS.map((label, index) => {
        const status = stepStatus(index, completedSteps, runningStep)
        const isLast = index === STEP_LABELS.length - 1

        return (
          <li key={label} className="flex flex-1 items-center">
            <div className="flex flex-col items-center gap-1">
              <div
                className={
                  'flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-medium ' +
                  (status === 'done'
                    ? 'bg-slate-900 text-white'
                    : status === 'current'
                    ? 'animate-pulse bg-blue-600 text-white'
                    : 'border border-slate-300 bg-white text-slate-400')
                }
              >
                {status === 'done' ? '✓' : index + 1}
              </div>
              <span
                className={
                  'text-xs ' + (status === 'pending' ? 'text-slate-400' : 'text-slate-900')
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
