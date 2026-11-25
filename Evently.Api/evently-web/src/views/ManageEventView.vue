<template>
  <div v-if="event">
    <RouterLink :to="'/events/' + event.id" class="link">&larr; Voltar ao evento</RouterLink>

    <div class="page-head mt-16">
      <div>
        <h1 class="page-title">Gerir evento</h1>
        <p class="page-sub">{{ event.title }}</p>
      </div>
      <span class="badge" :class="event.status === 'Published' ? 'badge-success' : 'badge-muted'">
        {{ event.status === 'Published' ? 'Publicado' : 'Rascunho' }}
      </span>
    </div>

    <p v-if="msg" class="alert" :class="ok ? 'alert-success' : 'alert-error'">{{ msg }}</p>

    <div class="detail-grid">
      <section class="card">
        <h2 class="section-title" style="font-size: 20px">Detalhes</h2>
        <form class="stack mt-16" @submit.prevent="guardarEvento">
          <div class="field" style="margin: 0">
            <label class="label">Título</label>
            <input v-model="form.title" class="input" required />
          </div>
          <div class="field" style="margin: 0">
            <label class="label">Descrição</label>
            <textarea v-model="form.description" required></textarea>
          </div>
          <div class="row">
            <div class="field" style="margin: 0">
              <label class="label">Data e hora</label>
              <input v-model="form.date" type="datetime-local" class="input" required />
            </div>
            <div class="field" style="margin: 0">
              <label class="label">Local</label>
              <input v-model="form.location" class="input" required />
            </div>
          </div>
          <div class="row">
            <div class="field" style="margin: 0">
              <label class="label">Categoria</label>
              <select v-model="form.category" class="input">
                <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
              </select>
            </div>
            <div class="field" style="margin: 0">
              <label class="label">Imagem (URL)</label>
              <input v-model="form.imageUrl" class="input" placeholder="https://..." />
            </div>
          </div>
          <div class="row">
            <div class="field" style="margin: 0">
              <label class="label">Preço base (R$)</label>
              <input v-model.number="form.price" type="number" step="0.01" class="input" required />
            </div>
            <div class="field" style="margin: 0">
              <label class="label">Total de ingressos</label>
              <input v-model.number="form.totalTickets" type="number" class="input" required />
            </div>
          </div>
          <div class="inline">
            <label class="inline" style="font-weight: 500">
              <input type="checkbox" v-model="form.isFeatured" /> Destacar na página inicial
            </label>
            <label class="inline" style="font-weight: 500">
              <input type="checkbox" :checked="form.status === 'Published'" @change="toggleStatus" /> Publicado
            </label>
          </div>
          <div class="inline">
            <button type="submit" class="btn btn-primary" :disabled="saving">Guardar alterações</button>
            <button type="button" class="btn btn-danger" @click="apagarEvento">Apagar evento</button>
          </div>
        </form>
      </section>

      <section class="card">
        <h2 class="section-title" style="font-size: 20px">Lotes de ingressos</h2>
        <div v-for="batch in batches" :key="batch.id" class="batch">
          <div>
            <div class="batch-name">{{ batch.name }}</div>
            <small>{{ batch.sold }}/{{ batch.quantity }} vendidos</small>
          </div>
          <div class="inline">
            <span class="batch-price">R$ {{ batch.price.toFixed(2) }}</span>
            <button class="btn btn-ghost btn-sm" @click="editarLote(batch)">Editar</button>
            <button class="btn btn-danger btn-sm" @click="apagarLote(batch.id)">Remover</button>
          </div>
        </div>

        <form class="stack mt-16" @submit.prevent="guardarLote">
          <h3 class="section-title" style="font-size: 15px">{{ editingBatchId ? 'Editar lote' : 'Novo lote' }}</h3>
          <div class="row">
            <div class="field" style="margin: 0">
              <label class="label">Nome</label>
              <input v-model="batchForm.name" class="input" required />
            </div>
            <div class="field" style="margin: 0">
              <label class="label">Preço (R$)</label>
              <input v-model.number="batchForm.price" type="number" step="0.01" class="input" required />
            </div>
          </div>
          <div class="field" style="margin: 0">
            <label class="label">Quantidade</label>
            <input v-model.number="batchForm.quantity" type="number" class="input" required />
          </div>
          <div class="row">
            <div class="field" style="margin: 0">
              <label class="label">Início da venda</label>
              <input v-model="batchForm.saleStart" type="datetime-local" class="input" />
            </div>
            <div class="field" style="margin: 0">
              <label class="label">Fim da venda</label>
              <input v-model="batchForm.saleEnd" type="datetime-local" class="input" />
            </div>
          </div>
          <div class="inline">
            <button type="submit" class="btn btn-primary btn-sm">{{ editingBatchId ? 'Atualizar lote' : 'Adicionar lote' }}</button>
            <button v-if="editingBatchId" type="button" class="btn btn-ghost btn-sm" @click="resetLoteForm">Cancelar</button>
          </div>
        </form>
      </section>
    </div>

    <section class="card mt-16">
      <h2 class="section-title" style="font-size: 20px">Cupons de desconto</h2>
      <div v-if="coupons.length === 0" class="muted mt-8">Sem cupons.</div>
      <div v-else class="mt-8">
        <div v-for="c in coupons" :key="c.id" class="batch">
          <div>
            <div class="batch-name">{{ c.code }}</div>
            <small>
              {{ c.discountType === 'percent' ? c.discountValue + '%' : 'R$ ' + c.discountValue.toFixed(2) }}
              · usado {{ c.usedCount }}{{ c.maxUses > 0 ? '/' + c.maxUses : '' }}
            </small>
          </div>
          <button class="btn btn-danger btn-sm" @click="apagarCupom(c.id)">Remover</button>
        </div>
      </div>

      <form class="stack mt-16" @submit.prevent="criarCupom">
        <div class="row">
          <div class="field" style="margin: 0">
            <label class="label">Código</label>
            <input v-model="couponForm.code" class="input" placeholder="PROMO10" required />
          </div>
          <div class="field" style="margin: 0">
            <label class="label">Tipo</label>
            <select v-model="couponForm.discountType" class="input">
              <option value="percent">Percentual (%)</option>
              <option value="fixed">Valor fixo (R$)</option>
            </select>
          </div>
        </div>
        <div class="row">
          <div class="field" style="margin: 0">
            <label class="label">Valor do desconto</label>
            <input v-model.number="couponForm.discountValue" type="number" step="0.01" class="input" required />
          </div>
          <div class="field" style="margin: 0">
            <label class="label">Limite de usos (0 = ilimitado)</label>
            <input v-model.number="couponForm.maxUses" type="number" class="input" />
          </div>
        </div>
        <div class="field" style="margin: 0">
          <label class="label">Expira em (opcional)</label>
          <input v-model="couponForm.expiresAt" type="datetime-local" class="input" />
        </div>
        <button type="submit" class="btn btn-ghost btn-sm">Criar cupom</button>
      </form>
    </section>
  </div>

  <div v-else class="empty">A carregar evento...</div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useEventStore } from '../stores/events'
import type { TicketBatch, Coupon } from '../types'

const route = useRoute()
const router = useRouter()
const eventStore = useEventStore()

const categories = ['Música', 'Tecnologia', 'Teatro', 'Desporto', 'Workshop', 'Gastronomia', 'Outros']

const event = ref(eventStore.currentEvent)
const batches = ref<TicketBatch[]>([])
const coupons = ref<Coupon[]>([])
const saving = ref(false)
const msg = ref('')
const ok = ref(false)

const form = reactive({
  title: '',
  description: '',
  date: '',
  location: '',
  price: 0,
  totalTickets: 0,
  category: 'Outros',
  imageUrl: '',
  isFeatured: false,
  status: 'Published',
})

const editingBatchId = ref('')
const batchForm = reactive({
  name: '',
  price: 0,
  quantity: 0,
  saleStart: '',
  saleEnd: '',
})

const couponForm = reactive({
  code: '',
  discountType: 'percent',
  discountValue: 10,
  maxUses: 0,
  expiresAt: '',
})

const toLocalInput = (iso?: string | null) => {
  if (!iso) return ''
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

const notify = (text: string, success = true) => {
  msg.value = text
  ok.value = success
}

const load = async () => {
  const id = route.params.id as string
  await eventStore.fetchEvent(id)
  event.value = eventStore.currentEvent
  if (event.value) {
    Object.assign(form, {
      title: event.value.title,
      description: event.value.description,
      date: toLocalInput(event.value.date),
      location: event.value.location,
      price: event.value.price,
      totalTickets: event.value.totalTickets,
      category: event.value.category ?? 'Outros',
      imageUrl: event.value.imageUrl ?? '',
      isFeatured: !!event.value.isFeatured,
      status: event.value.status ?? 'Published',
    })
  }
  await eventStore.fetchBatches(id)
  batches.value = eventStore.batches
  coupons.value = await eventStore.fetchCoupons(id)
}

const toggleStatus = (e: Event) => {
  form.status = (e.target as HTMLInputElement).checked ? 'Published' : 'Draft'
}

const guardarEvento = async () => {
  saving.value = true
  try {
    await eventStore.updateEvent(route.params.id as string, {
      ...form,
      date: new Date(form.date).toISOString(),
      imageUrl: form.imageUrl || null,
    })
    notify('Evento atualizado com sucesso.')
    await load()
  } catch (err: any) {
    notify(err?.response?.data?.message || 'Erro ao guardar.', false)
  } finally {
    saving.value = false
  }
}

const apagarEvento = async () => {
  if (!window.confirm('Apagar este evento? Esta ação é irreversível.')) return
  try {
    await eventStore.deleteEvent(route.params.id as string)
    router.push('/dashboard')
  } catch (err: any) {
    notify(err?.response?.data?.message || 'Erro ao apagar.', false)
  }
}

const resetLoteForm = () => {
  editingBatchId.value = ''
  Object.assign(batchForm, { name: '', price: 0, quantity: 0, saleStart: '', saleEnd: '' })
}

const editarLote = (batch: TicketBatch) => {
  editingBatchId.value = batch.id
  Object.assign(batchForm, {
    name: batch.name,
    price: batch.price,
    quantity: batch.quantity,
    saleStart: toLocalInput(batch.saleStart),
    saleEnd: toLocalInput(batch.saleEnd),
  })
}

const guardarLote = async () => {
  try {
    const payload = {
      eventId: route.params.id as string,
      name: batchForm.name,
      price: batchForm.price,
      quantity: batchForm.quantity,
      saleStart: batchForm.saleStart ? new Date(batchForm.saleStart).toISOString() : null,
      saleEnd: batchForm.saleEnd ? new Date(batchForm.saleEnd).toISOString() : null,
    }
    if (editingBatchId.value) await eventStore.updateBatch(editingBatchId.value, payload)
    else await eventStore.createBatch(payload)
    notify('Lote guardado.')
    resetLoteForm()
    await eventStore.fetchBatches(route.params.id as string)
    batches.value = eventStore.batches
  } catch (err: any) {
    notify(err?.response?.data?.message || 'Erro ao guardar o lote.', false)
  }
}

const apagarLote = async (id: string) => {
  if (!window.confirm('Remover este lote?')) return
  try {
    await eventStore.deleteBatch(id)
    await eventStore.fetchBatches(route.params.id as string)
    batches.value = eventStore.batches
    notify('Lote removido.')
  } catch (err: any) {
    notify(err?.response?.data?.message || 'Erro ao remover o lote.', false)
  }
}

const criarCupom = async () => {
  try {
    await eventStore.createCoupon({
      eventId: route.params.id as string,
      code: couponForm.code,
      discountType: couponForm.discountType,
      discountValue: couponForm.discountValue,
      maxUses: couponForm.maxUses,
      expiresAt: couponForm.expiresAt ? new Date(couponForm.expiresAt).toISOString() : null,
    })
    Object.assign(couponForm, { code: '', discountType: 'percent', discountValue: 10, maxUses: 0, expiresAt: '' })
    coupons.value = await eventStore.fetchCoupons(route.params.id as string)
    notify('Cupom criado.')
  } catch (err: any) {
    notify(err?.response?.data?.message || 'Erro ao criar o cupom.', false)
  }
}

const apagarCupom = async (id: string) => {
  try {
    await eventStore.deleteCoupon(id)
    coupons.value = await eventStore.fetchCoupons(route.params.id as string)
  } catch (err: any) {
    notify(err?.response?.data?.message || 'Erro ao remover o cupom.', false)
  }
}

onMounted(load)
</script>
