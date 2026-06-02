import { useQuery, useMutation } from "@tanstack/react-query";
import api from "@/lib/api";
import type { EngineeringActivity } from "./types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { 
  GitCommit, 
  GitPullRequest, 
  Clock, 
  ExternalLink, 
  Filter, 
  Activity, 
  Search, 
  Database, 
  ArrowRight,
  Sparkles,
  Loader2
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

function formatRelativeTime(date: Date) {
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
  
  if (diffInSeconds < 60) return "just now";
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
  return `${Math.floor(diffInSeconds / 86400)}d ago`;
}

export function EngineeringTimeline() {
  const [standup, setStandup] = useState<string | null>(null);
  const [isStandupOpen, setIsStandupOpen] = useState(false);

  const { data, isLoading } = useQuery<EngineeringActivity>({
    queryKey: ["engineering-activity"],
    queryFn: async () => {
      const response = await api.get("/engineering/activity");
      return response.data;
    },
    refetchInterval: 15000,
  });

  const standupMutation = useMutation({
    mutationFn: async () => {
      const response = await api.post("/engineering/standup");
      return response.data.summary;
    },
    onSuccess: (summary) => {
      setStandup(summary);
      setIsStandupOpen(true);
    },
  });

  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center bg-[#0F172A]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-[#06B6D4]"></div>
      </div>
    );
  }

  const timelineItems = [
    ...(data?.commits.map(c => ({ ...c, type: 'commit' as const, date: new Date(c.committedAt) })) || []),
    ...(data?.pullRequests.map(pr => ({ ...pr, type: 'pr' as const, date: new Date(pr.updatedAt) })) || [])
  ].sort((a, b) => b.date.getTime() - a.date.getTime());

  return (
    <div className="flex-1 overflow-y-auto bg-[#0F172A] relative selection:bg-[#06B6D4]/30">
      {/* Background Glow */}
      <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-[#06B6D4]/5 rounded-full blur-[120px] pointer-events-none" />

      <div className="max-w-6xl mx-auto p-8 relative z-10">
        {/* Unified Header */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 mb-12">
          <div className="space-y-1">
            <h2 className="text-3xl font-extrabold tracking-tight text-white flex items-center gap-3">
                <Activity className="h-8 w-8 text-[#06B6D4]" />
                Engineering Timeline
            </h2>
            <p className="text-slate-500 font-medium">Real-time signals from across your infrastructure.</p>
          </div>
          
          <div className="flex items-center gap-3">
             <Button 
                onClick={() => standupMutation.mutate()}
                disabled={standupMutation.isPending || timelineItems.length === 0}
                className="bg-[#06B6D4] hover:bg-[#0891B2] text-white font-bold rounded-xl px-6 gap-2 shadow-lg shadow-[#06B6D4]/20 transition-all"
             >
                {standupMutation.isPending ? (
                    <>
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Analyzing...
                    </>
                ) : (
                    <>
                        <Sparkles className="h-4 w-4" />
                        Generate AI Standup
                    </>
                )}
             </Button>

             <div className="h-6 w-px bg-[#334155] mx-2" />

             <div className="flex bg-[#1E293B] p-1 rounded-xl border border-[#334155]">
                <Button variant="ghost" size="sm" className="h-8 text-[10px] font-bold uppercase tracking-widest text-[#06B6D4] bg-[#06B6D4]/10 rounded-lg">
                    Live View
                </Button>
                <Button variant="ghost" size="sm" className="h-8 text-[10px] font-bold uppercase tracking-widest text-slate-500">
                    History
                </Button>
             </div>
             <Button variant="outline" size="sm" className="h-10 border-[#334155] bg-transparent text-slate-400 hover:text-white rounded-xl px-4 gap-2 transition-all">
                <Filter className="h-3.5 w-3.5" />
                Filter
             </Button>
          </div>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-6 mb-12">
            <div className="bg-[#1E293B]/40 border border-[#334155]/50 p-6 rounded-[1.5rem] backdrop-blur-sm">
                <p className="text-[10px] font-bold text-slate-500 uppercase tracking-widest mb-1">Total Commits</p>
                <h4 className="text-3xl font-bold text-white tracking-tight">{data?.commits.length || 0}</h4>
            </div>
            <div className="bg-[#1E293B]/40 border border-[#334155]/50 p-6 rounded-[1.5rem] backdrop-blur-sm">
                <p className="text-[10px] font-bold text-slate-500 uppercase tracking-widest mb-1">Active PRs</p>
                <h4 className="text-3xl font-bold text-white tracking-tight">{data?.pullRequests.length || 0}</h4>
            </div>
            <div className="bg-[#1E293B]/40 border border-[#334155]/50 p-6 rounded-[1.5rem] backdrop-blur-sm relative group cursor-pointer overflow-hidden">
                <div className="absolute inset-0 bg-[#06B6D4]/5 opacity-0 group-hover:opacity-100 transition-opacity" />
                <p className="text-[10px] font-bold text-slate-500 uppercase tracking-widest mb-1">AI Health Signal</p>
                <h4 className="text-3xl font-bold text-[#06B6D4] tracking-tight flex items-center gap-2">
                    Optimal
                    <div className="h-2 w-2 rounded-full bg-[#10B981] animate-pulse" />
                </h4>
            </div>
        </div>

        {/* Timeline List */}
        <div className="relative space-y-4 before:absolute before:inset-0 before:ml-5 before:-translate-x-px before:h-full before:w-0.5 before:bg-[#334155]/30">
          {timelineItems.length === 0 ? (
            <div className="text-center py-32 bg-[#1E293B]/20 rounded-[2rem] border border-[#334155] border-dashed">
              <div className="h-12 w-12 bg-[#1E293B] rounded-full flex items-center justify-center mx-auto mb-4 border border-[#334155]">
                 <Search className="h-6 w-6 text-slate-500" />
              </div>
              <p className="text-slate-500 font-medium">No engineering signals detected yet.</p>
            </div>
          ) : (
            timelineItems.map((item, index) => (
              <div key={index} className="relative flex items-start gap-8 group">
                {/* Node */}
                <div className={`absolute left-0 mt-3 flex h-10 w-10 items-center justify-center rounded-xl border border-[#334155] bg-[#0F172A] shadow-2xl group-hover:scale-110 group-hover:border-[#06B6D4] transition-all duration-300 z-10`}>
                  {item.type === 'commit' ? (
                    <GitCommit className="h-5 w-5 text-[#06B6D4]" />
                  ) : (
                    <GitPullRequest className={`h-5 w-5 ${item.state === 'merged' ? 'text-[#10B981]' : 'text-[#F59E0B]'}`} />
                  )}
                </div>

                <Card className="ml-12 flex-1 bg-[#1E293B]/20 hover:bg-[#1E293B]/40 transition-all duration-300 border-[#334155]/50 rounded-[1.5rem] overflow-hidden group/card hover:border-[#06B6D4]/30">
                  <CardHeader className="p-6 pb-4">
                    <div className="flex items-center justify-between mb-4">
                      <div className="flex items-center gap-2 text-[10px] font-bold text-slate-500 uppercase tracking-widest">
                        <span className="flex items-center gap-1.5 bg-[#0F172A] px-2.5 py-1 rounded-lg border border-[#334155] text-slate-300 group-hover/card:text-[#06B6D4] group-hover/card:border-[#06B6D4]/30 transition-colors">
                          <Database className="h-3 w-3" />
                          {item.repositoryName.split('/')[1]}
                        </span>
                        <span>•</span>
                        <span className="flex items-center gap-1.5 font-bold">
                          <Clock className="h-3 w-3" />
                          {formatRelativeTime(item.date)}
                        </span>
                      </div>
                      
                      <div className="flex items-center gap-2">
                        {item.type === 'commit' ? (
                            <div className="text-[10px] font-mono text-[#06B6D4] bg-[#06B6D4]/10 px-2 py-0.5 rounded border border-[#06B6D4]/20 uppercase">
                                {item.sha.substring(0, 7)}
                            </div>
                        ) : (
                            <div className={cn(
                                "text-[10px] font-bold px-2 py-0.5 rounded border uppercase tracking-tight",
                                item.state === 'open' ? 'bg-[#10B981]/10 text-[#10B981] border-[#10B981]/20' : 
                                item.state === 'merged' ? 'bg-[#06B6D4]/10 text-[#06B6D4] border-[#06B6D4]/20' : 
                                'bg-red-500/10 text-red-500 border-red-500/20'
                            )}>
                                {item.state}
                            </div>
                        )}
                        <a 
                            href={item.url} 
                            target="_blank" 
                            rel="noopener noreferrer"
                            className="h-7 w-7 rounded-lg border border-[#334155] flex items-center justify-center text-slate-500 hover:text-white hover:border-white transition-all"
                        >
                            <ExternalLink className="h-3.5 w-3.5" />
                        </a>
                      </div>
                    </div>
                    <CardTitle className="text-lg font-bold text-white leading-snug group-hover/card:text-[#06B6D4] transition-colors">
                      {item.type === 'commit' ? item.message : item.title}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="p-6 pt-0 text-slate-400">
                    <div className="flex items-center gap-4">
                      <div className="h-8 w-8 rounded-lg bg-slate-800 border border-[#334155] flex items-center justify-center text-[10px] font-bold text-white shrink-0 uppercase">
                        {item.authorName.substring(0, 2)}
                      </div>
                      <div className="flex flex-col">
                        <span className="text-sm font-bold text-slate-200">{item.authorName}</span>
                        <span className="text-[10px] font-bold text-slate-500 uppercase tracking-tight">System Engineer</span>
                      </div>
                      
                      <Button asChild variant="ghost" size="sm" className="ml-auto h-9 text-xs font-bold text-slate-400 hover:text-[#06B6D4] hover:bg-[#06B6D4]/10 rounded-xl gap-2 transition-all">
                        <a href={item.url} target="_blank" rel="noopener noreferrer">
                          {item.type === 'pr' ? `Review PR #${item.number}` : 'Inspect Artifacts'}
                          <ArrowRight className="h-3.5 w-3.5" />
                        </a>
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              </div>
            ))
          )}
        </div>
      </div>

      {/* AI Standup Dialog */}
      <Dialog open={isStandupOpen} onOpenChange={setIsStandupOpen}>
        <DialogContent className="max-w-2xl bg-[#1E293B] border-[#334155] text-white rounded-[2rem]">
          <DialogHeader>
            <DialogTitle className="text-2xl font-bold flex items-center gap-3">
                <div className="h-10 w-10 bg-[#06B6D4]/10 rounded-xl flex items-center justify-center">
                    <Sparkles className="h-6 w-6 text-[#06B6D4]" />
                </div>
                Daily Standup Summary
            </DialogTitle>
            <DialogDescription className="text-slate-400 font-medium">
                AI-generated summary of your engineering activity in the last 24 hours.
            </DialogDescription>
          </DialogHeader>
          
          <div className="mt-6 p-6 bg-[#0F172A] rounded-2xl border border-[#334155] max-h-[60vh] overflow-y-auto">
            <div className="prose prose-invert prose-slate max-w-none">
                {standup?.split('\n').map((line, i) => (
                    <p key={i} className="text-slate-300 leading-relaxed text-sm mb-4 last:mb-0">
                        {line}
                    </p>
                ))}
            </div>
          </div>

          <div className="mt-6 flex justify-end gap-3">
             <Button variant="outline" onClick={() => setIsStandupOpen(false)} className="border-[#334155] text-slate-400 hover:text-white rounded-xl bg-transparent">
                Close
             </Button>
             <Button 
                onClick={() => {
                    navigator.clipboard.writeText(standup || "");
                    alert("Copied to clipboard!");
                }}
                className="bg-[#06B6D4] hover:bg-[#0891B2] text-white font-bold rounded-xl"
             >
                Copy to Clipboard
             </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
