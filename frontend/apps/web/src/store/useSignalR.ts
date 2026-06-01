import { useEffect, useRef } from "react"
import * as signalR from "@microsoft/signalr"
import { useAuthStore } from "./useAuthStore"
import { useChatStore } from "./useChatStore"

export function useSignalR() {
  const connection = useRef<signalR.HubConnection | null>(null)
  const { token, isAuthenticated } = useAuthStore()
  const { addMessage, updateMessage, deleteMessage, activeChannel } = useChatStore()

  useEffect(() => {
    if (!isAuthenticated || !token) {
      if (connection.current) {
        connection.current.stop()
        connection.current = null
      }
      return
    }

    if (!connection.current) {
      connection.current = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub", {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning) // Silencing the non-critical info logs
        .build()

      connection.current.on("ReceiveMessage", (message: any) => {
        const channelId = message.channelId || message.ChannelId
        if (channelId) {
          addMessage(channelId, {
              id: message.id || message.Id,
              content: message.content || message.Content,
              username: message.username || message.Username,
              sentAt: message.sentAt || message.SentAt
          })
        }
      })

      connection.current.on("MessageUpdated", (message: any) => {
        const channelId = message.channelId || message.ChannelId
        if (channelId) {
          updateMessage(channelId, {
              id: message.id || message.Id,
              content: message.content || message.Content,
              username: message.username || message.Username,
              sentAt: message.sentAt || message.SentAt
          })
        }
      })

      connection.current.on("MessageDeleted", (messageId: string) => {
        if (activeChannel) {
            deleteMessage(activeChannel.id, messageId)
        }
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
  }, [isAuthenticated, token, addMessage, updateMessage, deleteMessage, activeChannel])

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
