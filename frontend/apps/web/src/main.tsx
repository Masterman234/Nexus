import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

const queryClient = new QueryClient()

console.log("%c >>> NEXUS_FRONTEND_MAIN_LOADED <<< ", "background: #f00; color: #fff; font-size: 20px; font-weight: bold;");

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
)
