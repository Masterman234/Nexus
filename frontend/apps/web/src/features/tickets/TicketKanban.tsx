import { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import api from "@/lib/api"
import { useChatStore } from "@/store/useChatStore"
import { useAuthStore } from "@/store/useAuthStore"
import { Button } from "@/components/ui/button"
import { 
  Plus, 
  MoreVertical, 
  Clock, 
  User as UserIcon,
  AlertCircle,
  CheckCircle2,
  Circle,
  HelpCircle,
  Layout,
  History,
  MessageSquare,
  GitPullRequest,
  GitCommit,
  Link as LinkIcon,
  type LucideIcon
} from "lucide-react"
import { cn } from "@/lib/utils"
import {
  Dialog,
  DialogContent,
} from "@/components/ui/dialog"

type TicketStatus = "Open" | "InProgress" | "Blocked" | "InReview" | "Done" | "Closed"

interface Ticket {
  id: string
  number: number
  title: string
  description: string
  status: TicketStatus
  priority: string
  assigneeUserId: string | null
  assigneeUsername: string | null
  creatorUserId: string
  workspaceId: string
  createdAt: string
  updatedAt: string
}

interface RelatedEntity {
  entityId: string
  entityType: string
  displayTitle: string
  referenceValue: string
  createdAt: string
  relationship: "Mentions" | "MentionedBy"
}

interface TicketHistory {
  id: string
  oldStatus: string | null
  newStatus: string
  createdAt: string
}

interface TicketComment {
  id: string
  content: string
  createdAt: string
}

interface TicketDetail {
  ticket: Ticket
  history: TicketHistory[]
  comments: TicketComment[]
}

const COLUMNS: { label: string; status: TicketStatus; icon: LucideIcon; color: string }[] = [
  { label: "Open", status: "Open", icon: Circle, color: "text-slate-400" },
  { label: "In Progress", status: "InProgress", icon: Clock, color: "text-blue-400" },
  { label: "Blocked", status: "Blocked", icon: AlertCircle, color: "text-red-400" },
  { label: "In Review", status: "InReview", icon: HelpCircle, color: "text-purple-400" },
  { label: "Done", status: "Done", icon: CheckCircle2, color: "text-emerald-400" }
]

export function TicketKanban() {
  const { channels } = useChatStore()
  const { user } = useAuthStore()
  const queryClient = useQueryClient()
  const [selectedTicketId, setSelectedTicketId] = useState<string | null>(null)
  const [editingDescription, setEditingDescription] = useState(false)
  const [descriptionValue, setDescriptionValue] = useState("")
  const [addingToColumn, setAddingToColumn] = useState<TicketStatus | null>(null)
  const [newTicketTitle, setNewTicketTitle] = useState("")
  const [newComment, setNewComment] = useState("")
  
  const workspaceId = channels[0]?.workspaceId

  const { data: tickets, isLoading } = useQuery<Ticket[]>({
    queryKey: ["tickets", workspaceId],
    queryFn: async () => {
      if (!workspaceId) return []
      const response = await api.get(`/tickets?workspaceId=${workspaceId}`)
      return response.data
    },
    enabled: !!workspaceId
  })

  const { data: users } = useQuery<{ id: string, username: string }[]>({
    queryKey: ["users"],
    queryFn: async () => {
      const response = await api.get("/users")
      return response.data
    }
  })

  const { data: ticketDetail, isLoading: isLoadingDetail } = useQuery({
    queryKey: ["ticket", selectedTicketId],
    queryFn: async (): Promise<TicketDetail | null> => {
      if (!selectedTicketId) return null
      const response = await api.get(`/tickets/${selectedTicketId}`)
      return response.data
    },
    enabled: !!selectedTicketId
  })

  const { data: relatedEntities, isLoading: isLoadingRelated } = useQuery<RelatedEntity[]>({
    queryKey: ["ticket-related", selectedTicketId],
    queryFn: async () => {
      if (!selectedTicketId) return []
      const response = await api.get(`/references/${selectedTicketId}/related`)
      return response.data
    },
    enabled: !!selectedTicketId
  })

  const updateStatusMutation = useMutation({
    mutationFn: async ({ ticketId, status }: { ticketId: string; status: TicketStatus }) => {
      return api.patch(`/tickets/${ticketId}/status`, {
        newStatus: status,
        userId: user?.id
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tickets"] })
      queryClient.invalidateQueries({ queryKey: ["ticket"] })
    }
  })

  const updateTicketMutation = useMutation({
    mutationFn: async ({ ticketId, title, description, priority }: { ticketId: string, title: string, description: string, priority: string }) => {
      return api.put(`/tickets/${ticketId}`, {
        title,
        description,
        priority
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tickets"] })
      queryClient.invalidateQueries({ queryKey: ["ticket"] })
      setEditingDescription(false)
    }
  })

  const assignTicketMutation = useMutation({
    mutationFn: async ({ ticketId, assigneeUserId }: { ticketId: string, assigneeUserId: string | null }) => {
      return api.patch(`/tickets/${ticketId}/assign`, {
        assigneeUserId
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tickets"] })
      queryClient.invalidateQueries({ queryKey: ["ticket"] })
    }
  })

  const createTicketMutation = useMutation({
    mutationFn: async ({ title, status }: { title: string, status: TicketStatus }) => {
      const response = await api.post('/tickets', {
        title,
        description: "",
        priority: "Medium",
        creatorUserId: user?.id,
        workspaceId
      });
      
      if (status !== "Open" && response.data.id) {
         await api.patch(`/tickets/${response.data.id}/status`, {
            newStatus: status,
            userId: user?.id
         });
      }
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tickets"] })
      setAddingToColumn(null)
      setNewTicketTitle("")
    }
  })

  const addCommentMutation = useMutation({
    mutationFn: async ({ ticketId, content }: { ticketId: string, content: string }) => {
      return api.post(`/tickets/${ticketId}/comments`, {
        userId: user?.id,
        content
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ticket"] })
      setNewComment("")
    }
  })

  if (!workspaceId) {
    return (
      <div className="flex-1 flex items-center justify-center text-slate-500">
        No workspace found. Create a channel first.
      </div>
    )
  }

  if (isLoading) {
    return <div className="flex-1 flex items-center justify-center animate-pulse text-[#06B6D4] font-bold uppercase tracking-widest">Initialising Board...</div>
  }

  return (
    <div className="flex-1 flex flex-col min-h-0 bg-[#0F172A] w-full">
      {/* Board Header */}
      <div className="h-14 border-b border-[#334155]/30 flex items-center px-4 justify-between bg-[#0F172A] shrink-0 relative z-20">
        <div className="flex items-center gap-3">
          <div className="h-5 w-5 rounded bg-[#06B6D4]/10 flex items-center justify-center">
              <Layout className="h-3.5 w-3.5 text-[#06B6D4]" />
          </div>
          <span className="font-bold text-[11px] sm:text-sm tracking-tight text-white uppercase tracking-widest">Ticket Board</span>
          <div className="h-3 w-px bg-[#334155] mx-1" />
          <span className="text-[10px] font-bold text-slate-500 uppercase tracking-widest hidden sm:inline">
            {tickets?.length || 0} Total Tickets
          </span>
        </div>
        <Button size="sm" className="bg-[#06B6D4] hover:bg-[#0891B2] text-white font-bold rounded-lg h-8 px-3 text-[10px] sm:text-xs gap-2 shadow-lg shadow-[#06B6D4]/10 shrink-0">
          <Plus className="h-3.5 w-3.5" />
          <span className="hidden xs:inline">New Ticket</span>
        </Button>
      </div>

      {/* Board Columns */}
      <div className="flex-1 flex gap-4 p-4 overflow-x-auto min-h-0 w-full scrollbar-thin scrollbar-thumb-slate-800 scrollbar-track-transparent">
        {COLUMNS.map(column => (
          <div key={column.status} className="w-72 sm:w-80 flex flex-col shrink-0">
            <div className="flex items-center justify-between mb-3 px-1">
              <div className="flex items-center gap-2">
                <column.icon className={cn("h-4 w-4", column.color)} />
                <span className="text-[11px] font-bold text-white uppercase tracking-wider">{column.label}</span>
                <span className="bg-[#1E293B] text-slate-400 text-[10px] px-1.5 py-0.5 rounded-md border border-[#334155]/50 font-bold">
                  {tickets?.filter(t => t.status === column.status).length || 0}
                </span>
              </div>
              <Button variant="ghost" size="icon" className="h-6 w-6 text-slate-500 hover:text-white">
                <MoreVertical className="h-3.5 w-3.5" />
              </Button>
            </div>

            <div className="flex-1 flex flex-col gap-3 min-h-0 overflow-y-auto pr-1 custom-scrollbar">
              {tickets?.filter(t => t.status === column.status).map(ticket => (
                <div 
                  key={ticket.id}
                  onClick={() => setSelectedTicketId(ticket.id)}
                  className="bg-[#1E293B] border border-[#334155]/50 rounded-xl p-4 hover:border-[#06B6D4]/50 transition-all cursor-pointer group relative overflow-hidden shadow-lg shadow-black/5"
                >
                  <div className="absolute top-0 left-0 w-1 h-full bg-transparent group-hover:bg-[#06B6D4] transition-all" />
                  
                  <div className="flex items-start justify-between mb-2">
                    <span className="text-[10px] font-bold text-[#06B6D4] uppercase tracking-tighter">NEX-{ticket.number}</span>
                    <div className={cn(
                      "text-[9px] font-bold px-1.5 py-0.5 rounded uppercase tracking-tighter border",
                      ticket.priority === "Urgent" ? "bg-red-400/10 text-red-400 border-red-400/20" :
                      ticket.priority === "High" ? "bg-orange-400/10 text-orange-400 border-orange-400/20" :
                      "bg-slate-400/10 text-slate-400 border-slate-400/20"
                    )}>
                      {ticket.priority}
                    </div>
                  </div>

                  <h4 className="text-sm font-bold text-white leading-tight mb-2 group-hover:text-[#06B6D4] transition-colors">{ticket.title}</h4>
                  
                  <div className="flex items-center justify-between mt-4 pt-3 border-t border-[#334155]/30">
                    <div className="flex items-center gap-2">
                      <div className={cn(
                        "h-5 w-5 rounded border border-[#334155] flex items-center justify-center",
                        ticket.assigneeUsername ? "bg-[#06B6D4]/10 border-[#06B6D4]/20" : "bg-slate-800"
                      )}>
                        <UserIcon className={cn("h-3 w-3", ticket.assigneeUsername ? "text-[#06B6D4]" : "text-slate-500")} />
                      </div>
                      <span className={cn(
                        "text-[10px] font-bold uppercase tracking-widest",
                        ticket.assigneeUsername ? "text-white" : "text-slate-500"
                      )}>
                        {ticket.assigneeUsername || "Unassigned"}
                      </span>
                    </div>
                    <span className="text-[10px] font-bold text-slate-600 uppercase tabular-nums tracking-tighter">
                      {new Date(ticket.createdAt).toLocaleDateString([], { month: 'short', day: 'numeric' })}
                    </span>
                  </div>
                </div>
              ))}

              {addingToColumn === column.status ? (
                <div className="bg-[#1E293B] border border-[#06B6D4]/50 rounded-xl p-3 shadow-lg shadow-black/5">
                  <input
                    type="text"
                    autoFocus
                    placeholder="Ticket title..."
                    value={newTicketTitle}
                    onChange={(e) => setNewTicketTitle(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' && newTicketTitle.trim()) {
                        createTicketMutation.mutate({ title: newTicketTitle, status: column.status })
                      } else if (e.key === 'Escape') {
                        setAddingToColumn(null)
                        setNewTicketTitle("")
                      }
                    }}
                    className="w-full bg-transparent border-none text-sm text-white focus:outline-none mb-3"
                  />
                  <div className="flex items-center justify-end gap-2">
                    <Button 
                      variant="ghost" 
                      size="sm" 
                      className="h-6 text-[10px] text-slate-400 hover:text-white px-2"
                      onClick={() => {
                        setAddingToColumn(null)
                        setNewTicketTitle("")
                      }}
                    >
                      Cancel
                    </Button>
                    <Button 
                      size="sm" 
                      className="h-6 text-[10px] bg-[#06B6D4] hover:bg-[#0891B2] text-white px-3"
                      disabled={!newTicketTitle.trim() || createTicketMutation.isPending}
                      onClick={() => createTicketMutation.mutate({ title: newTicketTitle, status: column.status })}
                    >
                      Create
                    </Button>
                  </div>
                </div>
              ) : (
                <Button 
                  variant="ghost" 
                  onClick={() => setAddingToColumn(column.status)}
                  className="w-full h-10 border border-dashed border-[#334155] hover:border-[#06B6D4]/50 hover:bg-[#06B6D4]/5 text-slate-500 hover:text-[#06B6D4] rounded-xl text-xs font-bold uppercase tracking-widest gap-2 shrink-0 mb-4"
                >
                  <Plus className="h-3.5 w-3.5" />
                  Add Item
                </Button>
              )}
            </div>
          </div>
        ))}
        {/* Extra space at the end to prevent clipping */}
        <div className="w-1 shrink-0" />
      </div>

      {/* Ticket Detail Modal */}
      <Dialog open={!!selectedTicketId} onOpenChange={(open) => !open && setSelectedTicketId(null)}>
        <DialogContent className="max-w-2xl bg-[#0F172A] border-[#334155] text-white p-0 overflow-hidden rounded-2xl shadow-2xl">
          {isLoadingDetail ? (
            <div className="h-96 flex items-center justify-center text-[#06B6D4] font-bold animate-pulse uppercase tracking-widest">Loading Details...</div>
          ) : ticketDetail ? (
            <div className="flex flex-col h-full max-h-[85vh]">
              {/* Modal Header */}
              <div className="px-6 py-4 border-b border-[#334155]/50 flex items-center justify-between bg-[#1E293B]/30">
                <div className="flex items-center gap-3">
                    <span className="text-xs font-bold text-[#06B6D4] uppercase tracking-widest bg-[#06B6D4]/10 px-2 py-1 rounded-md">NEX-{ticketDetail.ticket.number}</span>
                    <div className="h-4 w-px bg-[#334155]" />
                    <span className="text-[10px] font-bold text-slate-500 uppercase tracking-widest">Created {new Date(ticketDetail.ticket.createdAt).toLocaleDateString()}</span>
                </div>
                <div className="flex items-center gap-2">
                   <div className={cn(
                      "text-[10px] font-bold px-2 py-1 rounded uppercase tracking-widest border",
                      ticketDetail.ticket.priority === "Urgent" ? "bg-red-400/10 text-red-400 border-red-400/20" :
                      ticketDetail.ticket.priority === "High" ? "bg-orange-400/10 text-orange-400 border-orange-400/20" :
                      "bg-slate-400/10 text-slate-400 border-slate-400/20"
                    )}>
                      {ticketDetail.ticket.priority}
                    </div>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto p-6 space-y-8 custom-scrollbar">
                {/* Title & Description */}
                <div className="space-y-4">
                  <h2 className="text-2xl font-bold tracking-tight">{ticketDetail.ticket.title}</h2>
                  
                  {editingDescription ? (
                    <div className="space-y-2">
                      <textarea
                        value={descriptionValue}
                        onChange={(e) => setDescriptionValue(e.target.value)}
                        className="w-full h-32 p-4 bg-[#0F172A] border border-[#06B6D4]/50 rounded-xl text-sm text-slate-300 focus:outline-none focus:ring-1 focus:ring-[#06B6D4] resize-none"
                        placeholder="Add a description..."
                      />
                      <div className="flex items-center gap-2 justify-end">
                        <Button 
                          variant="ghost" 
                          size="sm"
                          onClick={() => setEditingDescription(false)}
                          className="text-slate-400 hover:text-white"
                        >
                          Cancel
                        </Button>
                        <Button 
                          size="sm"
                          className="bg-[#06B6D4] hover:bg-[#0891B2] text-white"
                          onClick={() => updateTicketMutation.mutate({
                            ticketId: ticketDetail.ticket.id,
                            title: ticketDetail.ticket.title,
                            description: descriptionValue,
                            priority: ticketDetail.ticket.priority
                          })}
                          disabled={updateTicketMutation.isPending}
                        >
                          {updateTicketMutation.isPending ? "Saving..." : "Save"}
                        </Button>
                      </div>
                    </div>
                  ) : (
                    <div 
                      className="p-4 bg-[#1E293B]/50 border border-[#334155]/50 rounded-xl cursor-text hover:border-[#334155] transition-colors min-h-[4rem]"
                      onClick={() => {
                        setDescriptionValue(ticketDetail.ticket.description || "")
                        setEditingDescription(true)
                      }}
                    >
                      <p className="text-sm text-slate-300 leading-relaxed whitespace-pre-wrap">
                          {ticketDetail.ticket.description || <span className="italic text-slate-500">No description provided. Click to add one.</span>}
                      </p>
                    </div>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-6">
                    {/* Status Picker */}
                    <div className="space-y-2">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-widest">Status</label>
                        <div className="flex flex-wrap gap-2">
                            {COLUMNS.map(c => (
                                <Button 
                                    key={c.status}
                                    size="sm"
                                    variant="ghost"
                                    onClick={() => updateStatusMutation.mutate({ ticketId: ticketDetail.ticket.id, status: c.status })}
                                    className={cn(
                                        "h-8 text-[10px] font-bold uppercase tracking-widest rounded-lg border",
                                        ticketDetail.ticket.status === c.status 
                                            ? "bg-[#06B6D4]/10 border-[#06B6D4]/50 text-[#06B6D4]" 
                                            : "border-[#334155] text-slate-500 hover:text-white"
                                    )}
                                >
                                    {c.label}
                                </Button>
                            ))}
                        </div>
                    </div>

                    {/* Assignee */}
                    <div className="space-y-2">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-widest">Assignee</label>
                        <div className="relative">
                          <select 
                            className="w-full appearance-none bg-slate-900/50 border border-[#334155]/50 rounded-xl px-12 py-3 text-sm font-bold text-slate-300 focus:outline-none focus:border-[#06B6D4]/50 cursor-pointer"
                            value={ticketDetail.ticket.assigneeUserId || ""}
                            onChange={(e) => assignTicketMutation.mutate({
                              ticketId: ticketDetail.ticket.id,
                              assigneeUserId: e.target.value === "" ? null : e.target.value
                            })}
                            disabled={assignTicketMutation.isPending}
                          >
                            <option value="">Unassigned</option>
                            {users?.map(u => (
                              <option key={u.id} value={u.id}>@{u.username}</option>
                            ))}
                          </select>
                          <div className="absolute left-3 top-1/2 -translate-y-1/2 h-6 w-6 rounded-md bg-slate-800 border border-[#334155] flex items-center justify-center pointer-events-none">
                              <UserIcon className={cn("h-3 w-3", ticketDetail.ticket.assigneeUserId ? "text-[#06B6D4]" : "text-slate-500")} />
                          </div>
                        </div>
                    </div>
                </div>

                {/* History Timeline */}
                <div className="space-y-4">
                   <div className="flex items-center gap-2 text-slate-400">
                     <History className="h-4 w-4" />
                     <span className="text-xs font-bold uppercase tracking-widest">Engineering History</span>
                   </div>
                   <div className="space-y-4 border-l-2 border-[#334155]/50 ml-2 pl-4 py-2">
                      {ticketDetail.history.map((h) => (
                        <div key={h.id} className="relative">
                            <div className="absolute -left-[21px] top-1 h-3 w-3 rounded-full bg-[#1E293B] border-2 border-[#334155]" />
                            <div className="text-[11px] font-medium">
                                <span className="text-slate-400">Changed status from</span>
                                <span className="text-white font-bold mx-1">`{h.oldStatus || "None"}`</span>
                                <span className="text-slate-400">to</span>
                                <span className="text-[#06B6D4] font-bold mx-1">`{h.newStatus}`</span>
                            </div>
                            <div className="text-[9px] font-bold text-slate-600 uppercase tracking-tighter mt-0.5">
                                {new Date(h.createdAt).toLocaleString()}
                            </div>
                        </div>
                      ))}
                   </div>
                </div>

                {/* Related Entities (Cross-Links) */}
                <div className="space-y-4">
                  <div className="flex items-center gap-2 text-slate-400">
                    <LinkIcon className="h-4 w-4" />
                    <span className="text-xs font-bold uppercase tracking-widest">Smart Context</span>
                  </div>
                  
                  {isLoadingRelated ? (
                    <div className="text-[10px] text-slate-600 animate-pulse font-bold uppercase">Scanning knowledge graph...</div>
                  ) : relatedEntities && relatedEntities.length > 0 ? (
                    <div className="grid grid-cols-1 gap-2">
                      {relatedEntities.map((rel) => (
                        <div 
                          key={`${rel.entityId}-${rel.relationship}`}
                          className="flex items-center justify-between p-3 bg-[#1E293B]/30 border border-[#334155]/30 rounded-xl hover:border-[#06B6D4]/30 transition-colors group"
                        >
                          <div className="flex items-center gap-3">
                            <div className="h-8 w-8 rounded-lg bg-slate-800 flex items-center justify-center text-[#06B6D4]">
                              {rel.entityType === "PullRequest" ? <GitPullRequest className="h-4 w-4" /> :
                               rel.entityType === "Commit" ? <GitCommit className="h-4 w-4" /> :
                               rel.entityType === "Message" ? <MessageSquare className="h-4 w-4" /> :
                               <LinkIcon className="h-4 w-4" />}
                            </div>
                            <div className="flex flex-col">
                              <span className="text-[10px] font-bold text-slate-500 uppercase tracking-tighter">
                                {rel.entityType} • {rel.relationship === "Mentions" ? "Linked in description" : "Mentioned in activity"}
                              </span>
                              <span className="text-sm font-bold text-white group-hover:text-[#06B6D4] transition-colors line-clamp-1">
                                {rel.displayTitle}
                              </span>
                            </div>
                          </div>
                          <span className="text-[10px] font-bold text-[#06B6D4] opacity-0 group-hover:opacity-100 transition-opacity">View →</span>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-[10px] text-slate-600 font-bold uppercase italic">No cross-links discovered yet.</p>
                  )}
                </div>

                 {/* Comments Section */}
                 <div className="space-y-4">
                    <div className="flex items-center gap-2 text-slate-400">
                        <MessageSquare className="h-4 w-4" />
                        <span className="text-xs font-bold uppercase tracking-widest">Discussion</span>
                    </div>
                    {ticketDetail.comments.length === 0 ? (
                        <p className="text-[10px] text-slate-600 font-bold uppercase italic">No comments yet.</p>
                    ) : (
                        <div className="space-y-3">
                            {ticketDetail.comments.map((c) => (
                                <div key={c.id} className="bg-slate-900/30 p-3 rounded-xl border border-[#334155]/30">
                                    <p className="text-xs text-slate-300">{c.content}</p>
                                    <div className="text-[9px] font-bold text-slate-600 uppercase mt-2">{new Date(c.createdAt).toLocaleString()}</div>
                                </div>
                            ))}
                        </div>
                    )}
                    <div className="flex gap-2 pt-2">
                        <input
                            type="text"
                            placeholder="Add a comment..."
                            value={newComment}
                            onChange={(e) => setNewComment(e.target.value)}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter' && newComment.trim()) {
                                    addCommentMutation.mutate({ ticketId: ticketDetail.ticket.id, content: newComment })
                                }
                            }}
                            className="flex-1 bg-slate-900/50 border border-[#334155]/50 rounded-xl px-4 py-2 text-sm text-slate-300 focus:outline-none focus:border-[#06B6D4]/50"
                        />
                        <Button 
                            size="sm"
                            className="bg-[#06B6D4] hover:bg-[#0891B2] text-white rounded-xl px-4"
                            disabled={!newComment.trim() || addCommentMutation.isPending}
                            onClick={() => addCommentMutation.mutate({ ticketId: ticketDetail.ticket.id, content: newComment })}
                        >
                            Post
                        </Button>
                    </div>
                 </div>
              </div>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  )
}
