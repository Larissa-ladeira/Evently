export interface User {
  id: string
  name: string
  email: string
  role: string
  token?: string
}

export interface Event {
  id: string
  title: string
  description: string
  date: string
  location: string
  price: number
  totalTickets: number
  category?: string
  imageUrl?: string | null
  isFeatured?: boolean
  status?: string
  organizerName?: string
  rating?: number | null
  reviewCount?: number
  sold?: number
  available?: number
  soldOut?: boolean
}

export interface TicketBatch {
  id: string
  eventId: string
  name: string
  price: number
  quantity: number
  sold: number
  available?: number
  soldOut?: boolean
  onSale?: boolean
  saleStart?: string | null
  saleEnd?: string | null
}

export interface FormField {
  label: string
  name: string
  type: string
  required: boolean
  options?: string[] | null
}

export interface EventForm {
  id?: string
  eventId: string
  fields: FormField[]
}

export interface Review {
  id: string
  rating: number
  comment?: string | null
  createdAt: string
  userName: string
}

export interface Coupon {
  id: string
  eventId: string
  code: string
  discountType: string
  discountValue: number
  maxUses: number
  usedCount: number
  expiresAt?: string | null
  isActive: boolean
}

export interface OrderItemPayload {
  ticketTypeId: string
  quantity: number
}

export interface EventFilters {
  q?: string
  category?: string
  sort?: string
  minPrice?: number | null
  maxPrice?: number | null
  from?: string | null
  to?: string | null
  featured?: boolean
}

export interface OrderEvent {
  id: string
  title: string
  date: string
  location: string
  imageUrl?: string | null
  category?: string
}

export interface OrderSummary {
  id: string
  status: string
  totalAmount: number
  discountAmount: number
  couponCode?: string | null
  createdAt: string
  ticketCount: number
  event?: OrderEvent | null
}

export interface OrderTicket {
  id: string
  ticketType: string
  status: string
  qrCode: string
  usedAt?: string | null
}

export interface OrderDetail {
  id: string
  status: string
  totalAmount: number
  discountAmount: number
  couponCode?: string | null
  createdAt: string
  paymentProvider?: string | null
  event?: OrderEvent | null
  buyer?: { name: string; email: string } | null
  items: { ticketTypeName: string; quantity: number; unitPrice: number; totalPrice: number }[]
  tickets: OrderTicket[]
}

export interface Ticket {
  id: string
  ticketType: string
  status: string
  qrCode: string
  qrCodeText: string
  event?: { id: string; title: string; date: string; location: string }
}
