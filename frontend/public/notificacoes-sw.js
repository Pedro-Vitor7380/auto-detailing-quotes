// Parte do service worker que cuida das notificações. O vite-plugin-pwa gera o service worker
// principal (cache do app) e importa este arquivo nele (vite.config.ts → workbox.importScripts).
// Roda em segundo plano, mesmo com o app fechado: é ele que mostra a notificação no celular.

// Chegou uma notificação da API (já descriptografada pelo navegador).
self.addEventListener('push', (evento) => {
  const dados = evento.data ? evento.data.json() : { titulo: 'Orçamentos', corpo: '', url: '/' }
  evento.waitUntil(
    self.registration.showNotification(dados.titulo, {
      body: dados.corpo,
      icon: '/icons/icon-192.png',
      badge: '/icons/favicon-64.png',
      data: { url: dados.url || '/' },
      tag: 'entregas-de-amanha', // uma notificação nova substitui a anterior, em vez de empilhar
    }),
  )
})

// Tocou na notificação: abre o app (ou traz para frente, se já estiver aberto) na tela indicada.
self.addEventListener('notificationclick', (evento) => {
  evento.notification.close()
  const url = new URL(evento.notification.data?.url || '/', self.location.origin).href
  evento.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((janelas) => {
      const aberta = janelas.find((j) => j.url.startsWith(self.location.origin))
      if (aberta) return aberta.focus().then(() => aberta.navigate(url))
      return self.clients.openWindow(url)
    }),
  )
})
