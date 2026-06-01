import { create } from "zustand"

export interface Message {
  id: string
  content: string
  username: string
  sentAt: string
}

export interface Channel {
  id: string
  name: string
  description: string
}

interface ChatState {
  channels: Channel[]
  activeChannel: Channel | null
  messages: Record<string, Message[]>
  setChannels: (channels: Channel[]) => void
  setActiveChannel: (channel: Channel | null) => void
  addMessage: (channelId: string, message: Message) => void
  updateMessage: (channelId: string, message: Message) => void
  deleteMessage: (channelId: string, messageId: string) => void
  setMessages: (channelId: string, messages: Message[]) => void
}

export const useChatStore = create<ChatState>((set) => ({
  channels: [],
  activeChannel: null,
  messages: {},
  setChannels: (channels) => set({ channels }),
  setActiveChannel: (channel) => set({ activeChannel: channel }),
  addMessage: (channelId, message) =>
    set((state) => {
      // EMERGENCY LOG
      console.log(`%c [STORE] New Message for ${channelId}:`, 'background: #222; color: #bada55', message)
      
      const channelMessages = state.messages[channelId] || []
      if (channelMessages.some((m) => m.id === message.id)) {
        return state
      }
      return {
        messages: {
          ...state.messages,
          [channelId]: [...channelMessages, message],
        },
      }
    }),
  updateMessage: (channelId, message) =>
    set((state) => ({
      messages: {
        ...state.messages,
        [channelId]: (state.messages[channelId] || []).map((m) =>
          m.id === message.id ? message : m
        ),
      },
    })),
  deleteMessage: (channelId, messageId) =>
    set((state) => ({
      messages: {
        ...state.messages,
        [channelId]: (state.messages[channelId] || []).filter(
          (m) => m.id !== messageId
        ),
      },
    })),
  setMessages: (channelId, messages) =>
    set((state) => ({
      messages: {
        ...state.messages,
        [channelId]: messages,
      },
    })),
}))
