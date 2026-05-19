document.addEventListener('DOMContentLoaded', () => {
  import('./init/bootstrap.js').then((m) => (m as { run: () => void }).run());
});
