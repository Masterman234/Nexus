import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { useMutation } from "@tanstack/react-query"
import { PlayCircle, ArrowRight } from "lucide-react"

import api from "@/lib/api"
import { useAuthStore, type User } from "@/store/useAuthStore"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"

interface AuthResponse {
  user: User
  // Backend (AuthResponse record) serializes the JWT as `accessToken`.
  accessToken: string
}

// Optional demo walkthrough video. Set VITE_DEMO_VIDEO_URL to a YouTube *embed*
// URL (e.g. https://www.youtube.com/embed/<id>) to enable the "Watch Demo" button.
// Left blank, the button is hidden so nothing broken is shown to visitors.
const DEMO_VIDEO_URL = import.meta.env.VITE_DEMO_VIDEO_URL as string | undefined

/**
 * Landing-page demo call-to-actions: one-click guest login ("Try the demo") and
 * an optional "Watch Demo" video modal. Guest login reuses the same auth-store
 * mechanism as the normal login form, so SignalR + protected routes work as-is.
 */
export function DemoActions() {
  const navigate = useNavigate()
  const setAuth = useAuthStore((state) => state.setAuth)
  const [videoOpen, setVideoOpen] = useState(false)

  const guestLogin = useMutation({
    mutationFn: async () => {
      const response = await api.post("/auth/guest")
      return response.data as AuthResponse
    },
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken)
      navigate("/app")
    },
  })

  return (
    <>
      <Button
        variant="outline"
        size="lg"
        onClick={() => guestLogin.mutate()}
        disabled={guestLogin.isPending}
        className="border-[#334155] hover:bg-[#1E293B] text-white font-bold rounded-2xl px-10 h-14 text-lg bg-transparent group"
      >
        {guestLogin.isPending ? (
          "Entering demo..."
        ) : (
          <span className="flex items-center gap-2">
            Try the Demo <ArrowRight className="h-5 w-5 group-hover:translate-x-1 transition-transform" />
          </span>
        )}
      </Button>

      {DEMO_VIDEO_URL && (
        <Button
          variant="outline"
          size="lg"
          onClick={() => setVideoOpen(true)}
          className="border-[#334155] hover:bg-[#1E293B] text-white font-bold rounded-2xl px-10 h-14 text-lg bg-transparent"
        >
          <span className="flex items-center gap-2">
            <PlayCircle className="h-5 w-5" /> Watch Demo
          </span>
        </Button>
      )}

      {guestLogin.isError && (
        <p className="text-sm text-red-400 font-medium w-full text-center sm:text-left">
          Demo is unavailable right now. Please try again shortly.
        </p>
      )}

      <Dialog open={videoOpen} onOpenChange={setVideoOpen}>
        <DialogContent className="max-w-4xl border-[#334155] bg-[#0F172A] text-white">
          <DialogHeader>
            <DialogTitle>Nexus — product walkthrough</DialogTitle>
            <DialogDescription className="text-slate-400">
              A guided tour of the cross-context engineering intelligence platform.
            </DialogDescription>
          </DialogHeader>
          <div className="relative w-full overflow-hidden rounded-xl" style={{ aspectRatio: "16 / 9" }}>
            {videoOpen && DEMO_VIDEO_URL && (
              <iframe
                className="absolute inset-0 h-full w-full"
                src={DEMO_VIDEO_URL}
                title="Nexus demo"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            )}
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
