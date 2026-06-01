import { useQuery } from "@tanstack/react-query";
import api from "@/lib/api";
import type { EngineeringActivity, Commit, PullRequest } from "./types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { GitCommit, GitPullRequest, GitBranch, Clock, User } from "lucide-react";

function formatRelativeTime(date: Date) {
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
  
  if (diffInSeconds < 60) return "just now";
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
  return `${Math.floor(diffInSeconds / 86400)}d ago`;
}

export function EngineeringTimeline() {
  const { data, isLoading } = useQuery<EngineeringActivity>({
    queryKey: ["engineering-activity"],
    queryFn: async () => {
      const response = await api.get("/engineering/activity");
      return response.data;
    },
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  // Merge and sort activity by date
  const timelineItems = [
    ...(data?.commits.map(c => ({ ...c, type: 'commit' as const, date: new Date(c.committedAt) })) || []),
    ...(data?.pullRequests.map(pr => ({ ...pr, type: 'pr' as const, date: new Date(pr.updatedAt) })) || [])
  ].sort((a, b) => b.date.getTime() - a.date.getTime());

  return (
    <div className="flex-1 overflow-y-auto p-6 bg-background/50">
      <div className="max-w-4xl mx-auto space-y-8">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Engineering Timeline</h2>
          <p className="text-muted-foreground">Real-time activity from your repositories.</p>
        </div>

        <div className="relative space-y-4 before:absolute before:inset-0 before:ml-5 before:-translate-x-px before:h-full before:w-0.5 before:bg-gradient-to-b before:from-transparent before:via-muted before:to-transparent">
          {timelineItems.length === 0 ? (
            <div className="text-center py-20">
              <p className="text-muted-foreground">No activity found yet. Connect a webhook to see data here!</p>
            </div>
          ) : (
            timelineItems.map((item, index) => (
              <div key={index} className="relative flex items-start gap-6 group">
                <div className="absolute left-0 mt-1.5 flex h-10 w-10 items-center justify-center rounded-full border bg-background shadow-sm group-hover:border-primary transition-colors">
                  {item.type === 'commit' ? (
                    <GitCommit className="h-5 w-5 text-blue-500" />
                  ) : (
                    <GitPullRequest className={`h-5 w-5 ${item.state === 'merged' ? 'text-purple-500' : 'text-green-500'}`} />
                  )}
                </div>

                <Card className="ml-10 flex-1 hover:shadow-md transition-shadow">
                  <CardHeader className="p-4 pb-2">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <GitBranch className="h-3 w-3" />
                          {item.repositoryName}
                        </span>
                        <span>•</span>
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {formatRelativeTime(item.date)}
                        </span>
                      </div>
                      {item.type === 'commit' ? (
                        <span className="text-[10px] font-mono bg-muted px-1.5 py-0.5 rounded uppercase">
                          {item.sha.substring(0, 7)}
                        </span>
                      ) : (
                        <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded uppercase ${
                          item.state === 'open' ? 'bg-green-100 text-green-700' : 
                          item.state === 'merged' ? 'bg-purple-100 text-purple-700' : 
                          'bg-red-100 text-red-700'
                        }`}>
                          {item.state}
                        </span>
                      )}
                    </div>
                    <CardTitle className="text-base mt-1">
                      {item.type === 'commit' ? item.message : item.title}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="p-4 pt-0">
                    <div className="flex items-center gap-2 mt-2">
                      <div className="h-6 w-6 rounded-full bg-muted flex items-center justify-center">
                        <User className="h-3 w-3 text-muted-foreground" />
                      </div>
                      <span className="text-sm font-medium">{item.authorName}</span>
                      {item.type === 'pr' && (
                        <a 
                          href={item.url} 
                          target="_blank" 
                          rel="noopener noreferrer"
                          className="text-xs text-primary hover:underline ml-auto"
                        >
                          View PR #{item.number}
                        </a>
                      )}
                    </div>
                  </CardContent>
                </Card>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
