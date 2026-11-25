<template>
  <div>
    <div class="page-head">
      <div>
        <h1 class="page-title">Criar evento</h1>
        <p class="page-sub">Define os detalhes, lotes de ingressos e o formulário de inscrição.</p>
      </div>
    </div>

    <form v-if="!eventId" class="card" @submit.prevent="handleCreate">
      <h2 class="section-title" style="font-size: 20px">Detalhes do evento</h2>
      <div class="stack mt-16">
        <div class="field">
          <label class="label">Título</label>
          <input v-model="title" type="text" class="input" placeholder="Nome do evento" required />
        </div>
        <div class="field">
          <label class="label">Descrição</label>
          <textarea v-model="description" placeholder="Descreve o teu evento" required></textarea>
        </div>
        <div class="row">
          <div class="field">
            <label class="label">Data e hora</label>
            <input v-model="date" type="datetime-local" class="input" required />
          </div>
          <div class="field">
            <label class="label">Local</label>
            <input v-model="location" type="text" class="input" placeholder="Cidade, espaço" required />
          </div>
        </div>
        <div class="row">
          <div class="field">
            <label class="label">Preço base (R$)</label>
            <input v-model.number="price" type="number" step="0.01" class="input" required />
          </div>
          <div class="field">
            <label class="label">Total de ingressos</label>
            <input v-model.number="totalTickets" type="number" class="input" required />
          </div>
        </div>
        <div class="row">
          <div class="field">
            <label class="label">Categoria</label>
            <select v-model="category" class="input">
              <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
            </select>
          </div>
          <div class="field">
            <label class="label">Imagem de capa (URL, opcional)</label>
            <input v-model="imageUrl" type="url" class="input" placeholder="https://..." />
          </div>
        </div>
        <label class="inline" style="font-weight: 500">
          <input type="checkbox" v-model="isFeatured" /> Destacar na página inicial
        </label>

        <p v-if="mensagem" class="alert" :class="sucesso ? 'alert-success' : 'alert-error'">
          {{ mensagem }}
        </p>

        <button type="submit" class="btn btn-primary" :disabled="criando">
          <AppIcon name="plus" />
          {{ criando ? 'A criar...' : 'Criar evento' }}
        </button>
      </div>
    </form>

    <template v-else>
      <p v-if="mensagem" class="alert" :class="sucesso ? 'alert-success' : 'alert-error'">
        {{ mensagem }}
      </p>

      <div class="detail-grid mt-16">
        <section class="card">
          <div class="section-head" style="margin-bottom: 10px">
            <h2 class="section-title" style="font-size: 20px">Lotes de ingressos</h2>
          </div>

          <p v-if="batches.length === 0" class="muted">Ainda sem lotes.</p>
          <div v-for="batch in batches" :key="batch.id" class="batch">
            <div>
              <div class="batch-name">{{ batch.name }}</div>
              <small>{{ batch.quantity }} unidades</small>
            </div>
            <span class="batch-price">R$ {{ batch.price.toFixed(2) }}</span>
          </div>

          <form class="stack mt-16" @submit.prevent="adicionarLote">
            <div class="row">
              <div class="field" style="margin: 0">
                <label class="label">Nome do lote</label>
                <input v-model="batchName" type="text" class="input" placeholder="Ex: Lote 1" required />
              </div>
              <div class="field" style="margin: 0">
                <label class="label">Preço (R$)</label>
                <input v-model.number="batchPrice" type="number" step="0.01" class="input" required />
              </div>
            </div>
            <div class="field" style="margin: 0">
              <label class="label">Quantidade</label>
              <input v-model.number="batchQuantity" type="number" class="input" required />
            </div>
            <button type="submit" class="btn btn-ghost">Adicionar lote</button>
          </form>
        </section>

        <section class="card">
          <h2 class="section-title" style="font-size: 20px">Formulário de inscrição</h2>
          <p class="muted mt-8">Campos personalizados pedidos no momento da compra.</p>

          <div v-for="(field, index) in fields" :key="index" class="field-item mt-16">
            <div class="inline">
              <input v-model="field.label" class="input grow" placeholder="Rótulo" />
              <input v-model="field.name" class="input grow" placeholder="Chave" />
              <button
                type="button"
                class="btn btn-danger btn-sm"
                title="Remover campo"
                @click="fields.splice(index, 1)"
              >
                <AppIcon name="trash" :size="15" />
              </button>
            </div>
            <div class="inline">
              <select v-model="field.type" class="input grow">
                <option value="text">Texto</option>
                <option value="number">Número</option>
                <option value="email">E-mail</option>
                <option value="date">Data</option>
                <option value="select">Seleção</option>
                <option value="checkbox">Checkbox</option>
              </select>
              <label class="inline" style="font-weight: 500">
                <input type="checkbox" v-model="field.required" /> Obrigatório
              </label>
            </div>
            <input
              v-if="field.type === 'select'"
              v-model="field.optionsText"
              class="input"
              placeholder="Opções separadas por vírgula"
            />
          </div>

          <div class="inline mt-16">
            <button type="button" class="btn btn-ghost btn-sm" @click="adicionarCampo">
              <AppIcon name="plus" :size="15" />
              Adicionar campo
            </button>
            <button
              type="button"
              class="btn btn-primary btn-sm"
              :disabled="fields.length === 0"
              @click="guardarFormulario"
            >
              Guardar formulário
            </button>
          </div>
        </section>
      </div>

      <div class="mt-24">
        <RouterLink :to="'/events/' + eventId" class="btn btn-primary">
          <AppIcon name="arrow" />
          Ver evento publicado
        </RouterLink>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useEventStore } from '../stores/events'
import type { FormField } from '../types'
import AppIcon from '../components/AppIcon.vue'

const eventStore = useEventStore()
const { batches } = storeToRefs(eventStore)

const title = ref('')
const description = ref('')
const date = ref('')
const location = ref('')
const price = ref(0)
const totalTickets = ref(0)
const category = ref('Outros')
const imageUrl = ref('')
const isFeatured = ref(false)
const categories = ['Música', 'Tecnologia', 'Teatro', 'Desporto', 'Workshop', 'Gastronomia', 'Outros']
const eventId = ref('')
const criando = ref(false)
const sucesso = ref(false)
const mensagem = ref('')

const batchName = ref('')
const batchPrice = ref(0)
const batchQuantity = ref(0)

interface FieldDraft extends FormField {
  optionsText: string
}
const fields = ref<FieldDraft[]>([])

const adicionarCampo = () => {
  fields.value.push({ label: '', name: '', type: 'text', required: false, options: null, optionsText: '' })
}

const handleCreate = async () => {
  criando.value = true
  sucesso.value = false
  mensagem.value = ''
  try {
    eventId.value = await eventStore.createEvent({
      title: title.value,
      description: description.value,
      date: new Date(date.value).toISOString(),
      location: location.value,
      price: price.value,
      totalTickets: totalTickets.value,
      category: category.value,
      imageUrl: imageUrl.value || null,
      isFeatured: isFeatured.value,
    })
    sucesso.value = true
    mensagem.value = 'Evento criado! Agora adiciona lotes e o formulário.'
    await eventStore.fetchBatches(eventId.value)
  } catch (err: any) {
    mensagem.value = err?.response?.data?.message || 'Erro ao criar evento.'
  } finally {
    criando.value = false
  }
}

const adicionarLote = async () => {
  try {
    await eventStore.createBatch({
      eventId: eventId.value,
      name: batchName.value,
      price: batchPrice.value,
      quantity: batchQuantity.value,
    })
    batchName.value = ''
    batchPrice.value = 0
    batchQuantity.value = 0
  } catch {
    sucesso.value = false
    mensagem.value = 'Erro ao adicionar lote.'
  }
}

const guardarFormulario = async () => {
  try {
    const payload: FormField[] = fields.value.map((f) => ({
      label: f.label,
      name: f.name,
      type: f.type,
      required: f.required,
      options:
        f.type === 'select'
          ? f.optionsText.split(',').map((o) => o.trim()).filter(Boolean)
          : null,
    }))
    await eventStore.saveForm(eventId.value, payload)
    sucesso.value = true
    mensagem.value = 'Formulário guardado com sucesso!'
  } catch {
    sucesso.value = false
    mensagem.value = 'Erro ao guardar o formulário.'
  }
}
</script>
