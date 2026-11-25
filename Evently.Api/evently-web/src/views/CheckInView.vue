<template>
  <div>
    <div class="page-head">
      <div>
        <h1 class="page-title">Check-in</h1>
        <p class="page-sub">Valida os ingressos dos participantes na portaria.</p>
      </div>
    </div>

    <div class="detail-grid">
      <section class="card">
        <div class="camera-box">
          <video ref="videoEl" class="camera-view" playsinline muted></video>
          <div v-if="!cameraOn" class="camera-overlay">
            <AppIcon name="scan" :size="34" />
            <p>Câmara desligada</p>
          </div>
        </div>
        <div class="inline mt-16">
          <button v-if="!cameraOn" class="btn btn-primary" :disabled="!cameraSupported" @click="startCamera">
            <AppIcon name="scan" :size="15" /> Abrir câmara
          </button>
          <button v-else class="btn btn-danger" @click="stopCamera">
            <AppIcon name="x" :size="15" /> Parar
          </button>
          <span v-if="!cameraSupported" class="muted">Câmara indisponível neste navegador — usa o código manual.</span>
        </div>

        <form class="stack mt-16" @submit.prevent="scan(qrcode)">
          <div class="field" style="margin: 0">
            <label class="label">Código do ingresso (QR)</label>
            <input v-model="qrcode" type="text" class="input" placeholder="Cola ou escreve o código" />
          </div>
          <div class="field" style="margin: 0">
            <label class="label">Dispositivo / portaria (opcional)</label>
            <input v-model="deviceInfo" type="text" class="input" placeholder="Ex: Porta A" />
          </div>
          <button type="submit" class="btn btn-primary" :disabled="processando || !qrcode">
            <AppIcon name="check" :size="15" />
            {{ processando ? 'A validar...' : 'Validar ingresso' }}
          </button>
        </form>

        <p v-if="mensagem" class="alert" :class="sucesso ? 'alert-success' : 'alert-error'">{{ mensagem }}</p>
        <p v-if="ultimoTicketId" class="muted mt-8">Ingresso: {{ ultimoTicketId }}</p>
      </section>

      <section class="card">
        <h2 class="section-title" style="font-size: 20px">Histórico desta sessão</h2>
        <p v-if="historico.length === 0" class="muted mt-8">Nenhum check-in nesta sessão.</p>
        <ul v-else class="list-clean mt-16">
          <li v-for="(item, index) in historico" :key="index">
            <code class="ticket-code">{{ item.qrcode.slice(0, 14) }}…</code>
            <span class="muted">{{ item.mensagem }}</span>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue'
import api from '../services/api'
import AppIcon from '../components/AppIcon.vue'

const qrcode = ref('')
const deviceInfo = ref('')
const mensagem = ref('')
const sucesso = ref(false)
const ultimoTicketId = ref('')
const processando = ref(false)
const historico = ref<{ qrcode: string; mensagem: string }[]>([])

const videoEl = ref<HTMLVideoElement | null>(null)
const cameraOn = ref(false)
const cameraSupported = ref(
  typeof window !== 'undefined' && 'BarcodeDetector' in window && !!navigator.mediaDevices
)
let stream: MediaStream | null = null
let rafId = 0

const scan = async (code: string) => {
  if (!code) return
  processando.value = true
  mensagem.value = ''
  ultimoTicketId.value = ''
  try {
    const res = await api.post('/api/CheckIn/scan', {
      qrCode: code,
      deviceInfo: deviceInfo.value || null,
    })
    sucesso.value = true
    mensagem.value = res.data.message
    ultimoTicketId.value = res.data.ticketId
    historico.value.unshift({ qrcode: code, mensagem: res.data.message })
    qrcode.value = ''
  } catch (err: any) {
    sucesso.value = false
    mensagem.value = err?.response?.data?.message || 'Erro ao realizar check-in.'
    historico.value.unshift({ qrcode: code, mensagem: mensagem.value })
  } finally {
    processando.value = false
  }
}

const detectLoop = async (detector: any) => {
  if (!cameraOn.value || !videoEl.value) return
  try {
    const codes = await detector.detect(videoEl.value)
    if (codes && codes.length > 0) {
      const value = codes[0].rawValue
      stopCamera()
      await scan(value)
      return
    }
  } catch {
    /* ignore frame errors */
  }
  rafId = requestAnimationFrame(() => detectLoop(detector))
}

const startCamera = async () => {
  try {
    stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: { ideal: 'environment' } },
    })
    if (!videoEl.value) return
    videoEl.value.srcObject = stream
    await videoEl.value.play()
    cameraOn.value = true

    const Detector = (window as any).BarcodeDetector
    const detector = new Detector({ formats: ['qr_code'] })
    detectLoop(detector)
  } catch (err: any) {
    mensagem.value = 'Não foi possível acessar a câmara: ' + (err?.message || 'permissão negada')
    sucesso.value = false
  }
}

const stopCamera = () => {
  cameraOn.value = false
  cancelAnimationFrame(rafId)
  if (stream) {
    stream.getTracks().forEach((t) => t.stop())
    stream = null
  }
}

onBeforeUnmount(stopCamera)
</script>
