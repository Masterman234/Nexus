$webhookUrl = "http://localhost:5001/api/v1/webhooks/github"

$pushPayload = @{
    repository = @{
        full_name = "nexus/platform"
    }
    pusher = @{
        name = "nexus-dev"
    }
    commits = @(
        @{
            id = [guid]::NewGuid().ToString().Replace("-", "").Substring(0, 20)
            message = "feat: add engineering timeline persistence layer"
            timestamp = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            author = @{
                name = "Nexus Developer"
                email = "dev@nexus.com"
            }
        },
        @{
            id = [guid]::NewGuid().ToString().Replace("-", "").Substring(0, 20)
            message = "fix: resolve signalr reconnection logic"
            timestamp = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            author = @{
                name = "Nexus Developer"
                email = "dev@nexus.com"
            }
        }
    )
} | ConvertTo-Json -Depth 10

$prPayload = @{
    action = "opened"
    repository = @{
        full_name = "nexus/platform"
    }
    pull_request = @{
        id = 99999
        number = 42
        title = "Implement AI Standup Generator foundations"
        body = "This PR adds the domain models and persistence layer for engineering activity."
        state = "open"
        html_url = "https://github.com/nexus/platform/pull/42"
        user = @{
            login = "nexus-dev"
        }
        created_at = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        updated_at = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
    }
} | ConvertTo-Json -Depth 10

Write-Host "Sending mock PUSH event..." -ForegroundColor Cyan
Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $pushPayload -ContentType "application/json" -Headers @{ "X-GitHub-Event" = "push" }

Write-Host "Sending mock PULL_REQUEST event..." -ForegroundColor Cyan
Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $prPayload -ContentType "application/json" -Headers @{ "X-GitHub-Event" = "pull_request" }

Write-Host "Done! Refresh the Engineering Timeline in your browser." -ForegroundColor Green
