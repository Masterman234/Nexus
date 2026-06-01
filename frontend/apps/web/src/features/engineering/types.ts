export interface Commit {
  id: string;
  sha: string;
  message: string;
  authorName: string;
  authorEmail: string;
  repositoryName: string;
  url: string;
  committedAt: string;
}

export interface PullRequest {
  id: string;
  externalId: number;
  number: number;
  title: string;
  description: string;
  state: string;
  url: string;
  repositoryName: string;
  authorName: string;
  createdAt: string;
  updatedAt: string;
  mergedAt: string | null;
}

export interface EngineeringActivity {
  commits: Commit[];
  pullRequests: PullRequest[];
}
