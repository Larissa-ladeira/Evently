import { defineStore } from 'pinia'
import api from '../services/api'
import type { Event } from './events'

export const useFavoriteStore = defineStore('favorites', {
  state: () => ({
    ids: [] as string[],
    events: [] as Event[],
    loaded: false,
  }),
  getters: {
    isFavorite: (state) => (eventId: string) => state.ids.includes(eventId),
    count: (state) => state.ids.length,
  },
  actions: {
    async load() {
      try {
        const res = await api.get('/api/Favorites/ids')
        this.ids = res.data
        this.loaded = true
      } catch {
        this.ids = []
      }
    },
    async fetchEvents() {
      const res = await api.get('/api/Favorites/my')
      this.events = res.data
    },
    async toggle(eventId: string) {
      const res = await api.post(`/api/Favorites/toggle/${eventId}`)
      if (res.data.favorited) {
        if (!this.ids.includes(eventId)) this.ids.push(eventId)
      } else {
        this.ids = this.ids.filter((id) => id !== eventId)
        this.events = this.events.filter((e) => e.id !== eventId)
      }
      return res.data.favorited as boolean
    },
  },
})
