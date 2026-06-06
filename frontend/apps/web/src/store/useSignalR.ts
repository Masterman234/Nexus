import { useEffect, useRef } from "react"
import * as signalR from "@microsoft/signalr"
import { useAuthStore } from "./useAuthStore"
import { useChatStore } from "./useChatStore"

// SignalR payloads arrive with either camelCase or PascalCase keys depending on
// the server serializer, so every field is modelled as optional in both casings.
interface MessagePayload {
  id?: string; Id?: string
  channelId?: string; ChannelId?: string
  content?: string; Content?: string
  username?: string; Username?: string
  sentAt?: string; SentAt?: string
}

interface ChannelPayload {
  id?: string; Id?: string
  name?: string; Name?: string
  description?: string; Description?: string
  workspaceId?: string; WorkspaceId?: string
}

export function useSignalR() {
  const connection = useRef<signalR.HubConnection | null>(null)
  const { token, isAuthenticated } = useAuthStore()
  const { addMessage, updateMessage, deleteMessage, activeChannel, addChannel } = useChatStore()

  useEffect(() => {
    if (!isAuthenticated || !token) {
      if (connection.current) {
        connection.current.stop()
        connection.current = null
      }
      return
    }

    if (!connection.current) {
      // In dev, force long-polling because Vite's WS proxy creates a brief race
      // where SignalR's first WebSocket attempt fires before the server has
      // registered the negotiated connection id. The retry succeeds — but the
      // browser logs the failed first attempt as an unsilenceable red error.
      // Long-polling avoids the race entirely. Prod uses real WebSockets via
      // the same-origin API host.
      const transport = import.meta.env.DEV
        ? signalR.HttpTransportType.LongPolling
        : signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling

      connection.current = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub", {
          accessTokenFactory: () => token,
          transport,
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      connection.current.on("ReceiveMessage", (message: MessagePayload) => {
        console.log("%c [SignalR] Received Message:", "color: #06B6D4; font-weight: bold", message)
        const channelId = message.channelId || message.ChannelId
        if (channelId) {
          addMessage(channelId, {
              id: message.id || message.Id || "",
              content: message.content || message.Content || "",
              username: message.username || message.Username || "",
              sentAt: message.sentAt || message.SentAt || ""
          })
        }
      })

      connection.current.on("MessageUpdated", (message: MessagePayload) => {
        const channelId = message.channelId || message.ChannelId
        if (channelId) {
          updateMessage(channelId, {
              id: message.id || message.Id || "",
              content: message.content || message.Content || "",
              username: message.username || message.Username || "",
              sentAt: message.sentAt || message.SentAt || ""
          })
        }
      })

      connection.current.on("MessageDeleted", (messageId: string) => {
        if (activeChannel) {
            deleteMessage(activeChannel.id, messageId)
        }
      })

      connection.current.on("ChannelCreated", (channel: ChannelPayload) => {
        console.log("%c [SignalR] Channel Created:", "color: #eab308; font-weight: bold", channel)
        addChannel({
          id: channel.id || channel.Id || "",
          name: channel.name || channel.Name || "",
          description: channel.description || channel.Description || "",
          workspaceId: channel.workspaceId || channel.WorkspaceId || ""
        })
      })

      connection.current
        .start()
        .then(() => {
          console.log("%c [Nexus] Connected to Real-time Hub", "color: #00ff00; font-weight: bold")
          if (activeChannel) {
            connection.current?.invoke("JoinChannel", activeChannel.id)
          }
        })
        .catch((err) => {
            // Only log if it's not a standard fallback error
            if (!err.message.includes("WebSocket")) {
                console.error("[SignalR] Connection Error: ", err)
            }
        })
    }
  }, [isAuthenticated, token, addMessage, updateMessage, deleteMessage, addChannel, activeChannel])

  useEffect(() => {
    if (connection.current?.state === signalR.HubConnectionState.Connected && activeChannel) {
        connection.current.invoke("JoinChannel", activeChannel.id)
    }
  }, [activeChannel])

  const joinChannel = async (channelId: string) => {
    if (connection.current?.state === signalR.HubConnectionState.Connected) {
      await connection.current.invoke("JoinChannel", channelId)
    }
  }

  const leaveChannel = async (channelId: string) => {
    if (connection.current?.state === signalR.HubConnectionState.Connected) {
      await connection.current.invoke("LeaveChannel", channelId)
    }
  }

  return { joinChannel, leaveChannel }
}
