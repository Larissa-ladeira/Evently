import { defineStore } from 'pinia'
import api from '../services/api'
import type { OrderSummary, OrderDetail } from '../types'

export type { OrderEvent, OrderSummary, OrderTicket, OrderDetail } from '../types'

export const useOrderStore = defineStore('orders', {
  state: () => ({
    orders: [] as OrderSummary[],
    current: null as OrderDetail | null,
    loading: false,
  }),
  actions: {
    async fetchMine() {
      this.loading = true
      try {
        const res = await api.get('/api/Orders/my')
        this.orders = res.data
      } finally {
        this.loading = false
      }
    },
    async fetchOrder(id: string) {
      const res = await api.get(`/api/Orders/${id}`)
      this.current = res.data
      return this.current
    },
    async cancelOrder(id: string) {
      const res = await api.post(`/api/Orders/${id}/cancel`)
      return res.data
    },
  },
})
