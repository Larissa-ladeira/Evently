import { defineStore } from 'pinia'
import api from '../services/api'
import type {
  Event,
  TicketBatch,
  FormField,
  EventForm,
  Review,
  Coupon,
  OrderItemPayload,
  EventFilters,
} from '../types'

export type {
  Event,
  TicketBatch,
  FormField,
  EventForm,
  Review,
  Coupon,
  OrderItemPayload,
  EventFilters,
} from '../types'

export const useEventStore = defineStore('events', {
  state: () => ({
    events: [] as Event[],
    featured: [] as Event[],
    mine: [] as Event[],
    currentEvent: null as Event | null,
    batches: [] as TicketBatch[],
    currentForm: null as EventForm | null,
    loading: false,
  }),
  actions: {
    async fetchEvents(filters: EventFilters = {}) {
      this.loading = true
      try {
        const params: Record<string, unknown> = {}
        if (filters.q) params.q = filters.q
        if (filters.category && filters.category !== 'Todos') params.category = filters.category
        if (filters.sort) params.sort = filters.sort
        if (filters.minPrice != null) params.minPrice = filters.minPrice
        if (filters.maxPrice != null) params.maxPrice = filters.maxPrice
        if (filters.from) params.from = filters.from
        if (filters.to) params.to = filters.to
        if (filters.featured) params.featured = true

        const res = await api.get('/api/Events', { params })
        this.events = res.data
      } finally {
        this.loading = false
      }
    },
    async fetchFeatured() {
      const res = await api.get('/api/Events', { params: { featured: true } })
      this.featured = res.data
    },
    async fetchMine() {
      const res = await api.get('/api/Events/mine')
      this.mine = res.data
    },
    async fetchEvent(id: string) {
      const res = await api.get(`/api/Events/${id}`)
      this.currentEvent = res.data
    },
    async createEvent(event: Partial<Event>): Promise<string> {
      const res = await api.post('/api/Events', event)
      return res.data.eventId
    },
    async updateEvent(id: string, event: Partial<Event>) {
      await api.put(`/api/Events/${id}`, event)
    },
    async deleteEvent(id: string) {
      await api.delete(`/api/Events/${id}`)
    },
    async fetchBatches(eventId: string) {
      const res = await api.get(`/api/TicketBatches/event/${eventId}`)
      this.batches = res.data
    },
    async createBatch(payload: {
      eventId: string
      name: string
      price: number
      quantity: number
      saleStart?: string | null
      saleEnd?: string | null
    }) {
      const res = await api.post('/api/TicketBatches', payload)
      return res.data
    },
    async updateBatch(
      id: string,
      payload: {
        eventId: string
        name: string
        price: number
        quantity: number
        saleStart?: string | null
        saleEnd?: string | null
      }
    ) {
      const res = await api.put(`/api/TicketBatches/${id}`, payload)
      return res.data
    },
    async deleteBatch(id: string) {
      await api.delete(`/api/TicketBatches/${id}`)
    },
    async fetchForm(eventId: string) {
      try {
        const res = await api.get(`/api/EventForms/${eventId}`)
        this.currentForm = res.data
      } catch (err: any) {
        if (err?.response?.status === 404) {
          this.currentForm = null
        } else {
          throw err
        }
      }
      return this.currentForm
    },
    async saveForm(eventId: string, fields: FormField[]) {
      const res = await api.post(`/api/EventForms/${eventId}`, { fields })
      this.currentForm = res.data
      return res.data
    },
    async checkout(
      eventId: string,
      items: OrderItemPayload[],
      formAnswers: Record<string, string>,
      couponCode?: string
    ) {
      const orderRes = await api.post('/api/Orders', {
        eventId,
        items,
        formAnswers,
        couponCode: couponCode || null,
      })
      const order = orderRes.data
      await api.post('/api/Payments/mock', { orderId: order.orderId })
      return order
    },
    async applyCoupon(eventId: string, code: string, subtotal: number) {
      const res = await api.post('/api/Coupons/apply', { eventId, code, subtotal })
      return res.data as { code: string; discount: number; message: string }
    },
    async fetchReviews(eventId: string) {
      const res = await api.get(`/api/Reviews/event/${eventId}`)
      return res.data as { average: number; count: number; reviews: Review[] }
    },
    async submitReview(eventId: string, rating: number, comment: string) {
      const res = await api.post(`/api/Reviews/event/${eventId}`, { rating, comment })
      return res.data
    },
    async fetchCoupons(eventId: string) {
      const res = await api.get(`/api/Coupons/event/${eventId}`)
      return res.data as Coupon[]
    },
    async createCoupon(payload: {
      eventId: string
      code: string
      discountType: string
      discountValue: number
      maxUses: number
      expiresAt?: string | null
    }) {
      const res = await api.post('/api/Coupons', payload)
      return res.data
    },
    async deleteCoupon(id: string) {
      await api.delete(`/api/Coupons/${id}`)
    },
  },
})
