import { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import api from "@/lib/api"
import { useAuthStore } from "@/store/useAuthStore"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { LayoutDashboard, Plus, Loader2 } from "lucide-react"

interface Workspace {
  id: string
  name: string
  description: string
}

export function WorkspaceSelector() {
  const [isOpen, setIsOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [newName, setNewName] = useState("")
  const [newDescription, setNewDescription] = useState("")
  
  const queryClient = useQueryClient()
  const { user } = useAuthStore()

  const { data: workspaces, isLoading } = useQuery({
    queryKey: ["workspaces"],
    queryFn: async (): Promise<Workspace[]> => {
      const response = await api.get("/workspaces")
      return response.data
    }
  })

  const createMutation = useMutation({
    mutationFn: async () => {
      const response = await api.post("/workspaces", {
        name: newName,
        description: newDescription,
        ownerId: user?.id
      })
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workspaces"] })
      setIsCreating(false)
      setNewName("")
      setNewDescription("")
    }
  })

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault()
    if (!newName.trim()) return
    createMutation.mutate()
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button variant="ghost" size="sm" className="gap-2">
          <LayoutDashboard className="h-4 w-4" />
          Workspaces
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>{isCreating ? "Create Workspace" : "Your Workspaces"}</DialogTitle>
          <DialogDescription>
            {isCreating 
              ? "Give your new workspace a name and description." 
              : "Select a workspace to view its channels and collaborate."}
          </DialogDescription>
        </DialogHeader>

        {isCreating ? (
          <form onSubmit={handleCreate} className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                placeholder="Engineering Team"
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                required
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="description">Description</Label>
              <Input
                id="description"
                placeholder="Our primary collaboration hub"
                value={newDescription}
                onChange={(e) => setNewDescription(e.target.value)}
              />
            </div>
            <DialogFooter className="gap-2 sm:gap-0">
              <Button type="button" variant="ghost" onClick={() => setIsCreating(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Create Workspace
              </Button>
            </DialogFooter>
          </form>
        ) : (
          <div className="grid gap-4 py-4 max-h-[60vh] overflow-y-auto pr-2">
            {isLoading ? (
              <div className="flex justify-center py-8">
                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              </div>
            ) : (
              <>
                {workspaces?.map((workspace) => (
                  <Button
                    key={workspace.id}
                    variant="outline"
                    className="w-full justify-start h-16 px-4 gap-4"
                    onClick={() => {
                      console.log("Selected Workspace:", workspace.id)
                      setIsOpen(false)
                    }}
                  >
                    <div className="h-10 w-10 bg-primary/10 rounded flex items-center justify-center text-primary font-bold">
                      {workspace.name.substring(0, 1).toUpperCase()}
                    </div>
                    <div className="flex flex-col items-start text-left overflow-hidden">
                      <span className="font-bold truncate w-full">{workspace.name}</span>
                      <span className="text-xs text-muted-foreground truncate w-full">{workspace.description}</span>
                    </div>
                  </Button>
                ))}
                {workspaces?.length === 0 && (
                  <p className="text-center text-sm text-muted-foreground py-4">
                    No workspaces found.
                  </p>
                )}
                <Button 
                  variant="ghost" 
                  className="w-full gap-2 border-dashed border-2 h-16 mt-2 hover:bg-primary/5 hover:border-primary/50"
                  onClick={() => setIsCreating(true)}
                >
                  <Plus className="h-4 w-4" />
                  Create New Workspace
                </Button>
              </>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
