// Progressive, page-local wellbeing interactions. Motion follows the device preference.
(function () {
  'use strict';
  var media = window.matchMedia('(prefers-reduced-motion: reduce)');
  function reducedMotion() { return media.matches; }

  function applyMotion() {
    var reduced = reducedMotion();
    document.documentElement.setAttribute('data-motion', reduced ? 'off' : 'auto');
    document.dispatchEvent(new CustomEvent('mindwell:motionchange', { detail: { reduced: reduced } }));
  }

  function setupBreathing(card) {
    var start = card.querySelector('[data-breathing-start]');
    var reset = card.querySelector('[data-breathing-reset]');
    var status = card.querySelector('[data-breathing-status]');
    var time = card.querySelector('[data-breathing-time]');
    var disc = card.querySelector('.breathing-disc');
    var running = false;
    var elapsed = 0;
    var started = 0;
    var frame = 0;
    var duration = 60000;
    card.querySelector('[data-breathing-controls]').hidden = false;

    function announce(text) {
      // Update live text at phase boundaries, never once per animation frame.
      if (status.textContent !== text) status.textContent = text;
    }

    function currentElapsed() {
      return Math.min(duration, elapsed + (running ? performance.now() - started : 0));
    }

    function finish() {
      running = false;
      elapsed = duration;
      card.setAttribute('data-state', 'complete');
      start.textContent = 'Start again';
      time.textContent = 'One minute, just for you';
      announce('Take your time. Return to your natural breathing.');
      disc.style.transform = '';
      cancelAnimationFrame(frame);
    }

    function render() {
      if (!running) return;
      var now = currentElapsed();
      if (now >= duration) { finish(); return; }
      var cycle = now % 10000;
      var inhale = cycle < 4000;
      announce(inhale ? 'Breathe in gently' : 'Breathe out slowly');
      var label = Math.ceil((duration - now) / 1000) + ' seconds remaining';
      if (time.textContent !== label) time.textContent = label;
      if (!reducedMotion()) {
        var progress = inhale ? cycle / 4000 : 1 - (cycle - 4000) / 6000;
        var smooth = (1 - Math.cos(Math.PI * progress)) / 2;
        disc.style.transform = 'scale(' + (0.72 + 0.28 * smooth).toFixed(4) + ')';
      } else {
        disc.style.transform = '';
      }
      frame = requestAnimationFrame(render);
    }

    function pause() {
      if (!running) return;
      elapsed = currentElapsed();
      if (elapsed >= duration) { finish(); return; }
      running = false;
      cancelAnimationFrame(frame);
      card.setAttribute('data-state', 'paused');
      start.textContent = 'Resume';
      announce('Paused. Continue whenever you are ready.');
    }

    start.addEventListener('click', function () {
      if (running) { pause(); return; }
      if (elapsed >= duration) elapsed = 0;
      started = performance.now();
      running = true;
      card.setAttribute('data-state', 'running');
      start.textContent = 'Pause';
      reset.disabled = false;
      render();
    });
    reset.addEventListener('click', function () {
      running = false;
      elapsed = 0;
      cancelAnimationFrame(frame);
      card.setAttribute('data-state', 'idle');
      start.textContent = 'Start breathing';
      reset.disabled = true;
      announce('Ready when you are');
      time.textContent = '60 seconds';
      disc.style.transform = '';
    });
    document.addEventListener('visibilitychange', function () { if (document.hidden) pause(); });
    window.addEventListener('pagehide', pause);
    document.addEventListener('mindwell:motionchange', function () {
      if (reducedMotion()) disc.style.transform = '';
    });
    if ('IntersectionObserver' in window) {
      new IntersectionObserver(function (entries) {
        if (!entries[0].isIntersecting) pause();
      }).observe(card);
    }
  }

  function setupMoodSelection() {
    var picker = document.querySelector('.mood-option-grid');
    var note = document.querySelector('[data-mood-note]');
    if (!picker || !note) return;
    var messages = {
      '1': 'A difficult day belongs here too. Go gently with yourself.',
      '2': 'Thank you for making a little space for yourself today.',
      '3': 'An ordinary day is worth noticing, too.',
      '4': 'Take a moment to notice what felt good today.',
      '5': 'Make a little room for what brought you joy.'
    };
    picker.addEventListener('change', function (event) {
      if (event.target.name === 'moodLevel') note.textContent = messages[event.target.value] || 'Every feeling has a place here.';
    });
  }

  function setupMoodChart() {
    var canvas = document.getElementById('moodChart');
    var source = document.getElementById('mood-data');
    if (!canvas || !source) return;
    var summary = document.getElementById('moodChartSummary');
    var empty = document.getElementById('moodChartEmpty');
    var wrap = canvas.closest('.mood-chart-wrap');
    var scope = canvas.closest('.mood-chart-card');
    var chart;
    var days = 7;
    var rows;
    var names = { 1: 'Very low', 2: 'Low', 3: 'Okay', 4: 'Good', 5: 'Great' };
    try { rows = JSON.parse(source.textContent); }
    catch (error) {
      wrap.hidden = true;
      summary.textContent = 'The chart could not be loaded. Your saved entries are available in the table below.';
      return;
    }

    function draw(animate) {
      var today = new Date();
      today.setHours(0, 0, 0, 0);
      var cutoff = new Date(today);
      cutoff.setDate(today.getDate() - days + 1);
      var visible = rows.filter(function (row) {
        var parts = row.label.split('-').map(Number);
        var date = new Date(parts[0], parts[1] - 1, parts[2]);
        return date >= cutoff && date <= today;
      });
      empty.hidden = visible.length > 0;
      wrap.hidden = !visible.length || !window.Chart;
      summary.textContent = visible.length + (visible.length === 1 ? ' saved check-in' : ' saved check-ins') + ' in the last ' + days + ' days.';
      if (!window.Chart) {
        summary.textContent += ' View your entries in the table below.';
        return;
      }
      if (!visible.length) return;
      var styles = getComputedStyle(document.documentElement);
      var ink = styles.getPropertyValue('--text-secondary').trim();
      var accent = styles.getPropertyValue('--color-primary').trim();
      var grid = styles.getPropertyValue('--border-light').trim();
      var data = {
        labels: visible.map(function (row) { return row.label; }),
        datasets: [{ label: 'Mood', data: visible.map(function (row) { return Number(row.value); }), borderColor: accent,
          backgroundColor: styles.getPropertyValue('--color-primary-light').trim(), pointBackgroundColor: accent,
          borderWidth: 2, pointRadius: 4, pointHoverRadius: 6, tension: 0.25, fill: true }]
      };
      var options = {
        responsive: true, maintainAspectRatio: false, animation: reducedMotion() ? false : { duration: 450 },
        font: { family: 'Lexend, sans-serif' },
        plugins: { legend: { display: false }, tooltip: { callbacks: { label: function (item) { return names[item.parsed.y]; } } } },
        scales: {
          y: { min: 1, max: 5, ticks: { stepSize: 1, color: ink, callback: function (value) { return names[value]; }, font: { family: 'Lexend' } }, grid: { color: grid } },
          x: { ticks: { color: ink, maxTicksLimit: 7, font: { family: 'Lexend' } }, grid: { display: false } }
        }
      };
      if (chart) {
        chart.data = data;
        chart.options = options;
        chart.update(animate && !reducedMotion() ? undefined : 'none');
      } else {
        chart = new Chart(canvas, { type: 'line', data: data, options: options });
      }
    }
    scope.querySelectorAll('[data-range]').forEach(function (button) {
      button.addEventListener('click', function () {
        days = button.getAttribute('data-range') === 'monthly' ? 30 : 7;
        scope.querySelectorAll('[data-range]').forEach(function (item) {
          var active = item === button;
          item.classList.toggle('chart-filter-btn--active', active);
          item.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
        draw(true);
      });
    });
    document.addEventListener('mindwell:themechange', function () { draw(false); });
    document.addEventListener('mindwell:motionchange', function () { draw(false); });
    if (document.fonts) document.fonts.ready.then(function () { draw(false); });
    draw(false);
  }

  function setupScenes() {
    var scenes = document.querySelectorAll('[data-scene]');
    if ('IntersectionObserver' in window) {
      var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          entry.target.classList.toggle('scene-paused', !entry.isIntersecting);
          if (entry.isIntersecting) entry.target.classList.add('scene-seen');
        });
      }, { threshold: 0.15 });
      scenes.forEach(function (scene) { observer.observe(scene); });
    }
    function updateVisibility() {
      document.documentElement.toggleAttribute('data-document-hidden', document.hidden);
    }
    document.addEventListener('visibilitychange', updateVisibility);
    updateVisibility();
  }

  function setupLibraries() {
    document.querySelectorAll('[data-library]').forEach(function (root) {
      var controls = root.querySelector('[data-library-controls]');
      var items = Array.from(root.querySelectorAll('[data-library-item]'));
      if (!controls || !items.length) return;
      var search = root.querySelector('[data-library-search]');
      var format = root.querySelector('[data-kind-filter]');
      var chips = root.querySelectorAll('[data-topic-filter]');
      var count = root.querySelector('[data-library-count]');
      var empty = root.querySelector('[data-library-empty]');
      var topic = '';
      var limit = 6;
      var matches = items;
      var noun = search.id.indexOf('resources') === 0 ? 'resources' : 'articles';
      var moreRow = document.createElement('div');
      moreRow.className = 'library-more-row';
      var more = document.createElement('button');
      more.type = 'button';
      more.className = 'btn btn-outline';
      more.setAttribute('data-library-more', '');
      moreRow.appendChild(more);
      root.querySelector('.library-grid').insertAdjacentElement('afterend', moreRow);
      items.forEach(function (item, index) {
        var heading = item.querySelector('h2');
        heading.id = noun + '-item-' + index;
        item.setAttribute('aria-labelledby', heading.id);
      });
      controls.hidden = false;

      function update() {
        var query = search.value.trim().toLocaleLowerCase();
        var formatKey = format ? format.value.trim().toUpperCase() : '';
        matches = items.filter(function (item) {
          var text = Array.from(item.querySelectorAll('h2, .library-meta, .library-excerpt')).map(function (part) { return part.textContent; }).join(' ').toLocaleLowerCase();
          return (!topic || item.getAttribute('data-topic') === topic) &&
            (!formatKey || (item.getAttribute('data-kind') || '').trim().toUpperCase() === formatKey) && text.indexOf(query) >= 0;
        });
        var shown = new Set(matches.slice(0, limit));
        items.forEach(function (item) { item.hidden = !shown.has(item); });
        empty.hidden = matches.length > 0;
        moreRow.hidden = matches.length <= limit;
        more.textContent = 'Show ' + Math.min(6, Math.max(0, matches.length - limit)) + ' more ' + noun;
        var message = 'Showing ' + shown.size + ' of ' + matches.length + ' ' + noun;
        if (count.textContent !== message) count.textContent = message;
      }
      function resetLimit() { limit = 6; update(); }
      search.addEventListener('input', resetLimit);
      search.addEventListener('keydown', function (event) {
        // Search filters this server-rendered collection; Enter must not submit Web Forms.
        if (event.key === 'Enter') { event.preventDefault(); resetLimit(); }
      });
      if (format) format.addEventListener('change', resetLimit);
      chips.forEach(function (chip) {
        chip.addEventListener('click', function () {
          topic = chip.getAttribute('data-topic-filter');
          chips.forEach(function (other) { other.setAttribute('aria-pressed', other === chip ? 'true' : 'false'); });
          resetLimit();
        });
      });
      root.querySelector('[data-library-reset]').addEventListener('click', function () {
        search.value = '';
        if (format) format.value = '';
        topic = '';
        chips.forEach(function (chip) { chip.setAttribute('aria-pressed', chip.getAttribute('data-topic-filter') === '' ? 'true' : 'false'); });
        resetLimit();
      });
      more.addEventListener('click', function () {
        var firstNew = matches[limit];
        limit += 6;
        update();
        if (firstNew) {
          firstNew.setAttribute('tabindex', '-1');
          firstNew.focus({ preventScroll: true });
          firstNew.scrollIntoView({ behavior: reducedMotion() ? 'auto' : 'smooth', block: 'start' });
        }
      });
      update();
    });
  }

  function init() {
    if (window.Chart) {
      Chart.defaults.font.family = 'Lexend, sans-serif';
      function refreshSharedCharts() {
        var styles = getComputedStyle(document.documentElement);
        var ink = styles.getPropertyValue('--text-secondary').trim();
        Object.keys(Chart.instances).forEach(function (key) {
          var chart = Chart.instances[key];
          if (!chart.canvas || chart.canvas.id === 'moodChart') return;
          chart.options.animation = reducedMotion() ? false : { duration: 450 };
          var legend = chart.options.plugins && chart.options.plugins.legend;
          if (legend && legend.labels) legend.labels.color = ink;
          Object.keys(chart.options.scales || {}).forEach(function (axis) {
            var scale = chart.options.scales[axis];
            if (scale.ticks) scale.ticks.color = ink;
            if (scale.grid) scale.grid.color = styles.getPropertyValue('--border-light').trim();
          });
          chart.update('none');
        });
      }
      document.addEventListener('mindwell:themechange', refreshSharedCharts);
      document.addEventListener('mindwell:motionchange', refreshSharedCharts);
      if (document.fonts) document.fonts.ready.then(refreshSharedCharts);
    }
    if (media.addEventListener) media.addEventListener('change', applyMotion);
    else media.addListener(applyMotion);
    applyMotion();
    document.querySelectorAll('[data-breathing]').forEach(setupBreathing);
    setupScenes();
    setupLibraries();
    // Fresh pages stay collapsed. A row exists only after an explicit edit request.
    document.querySelectorAll('details .editing-row').forEach(function (row) { row.closest('details').open = true; });
    document.querySelectorAll('.article-cover-image').forEach(function (image) {
      function hideUnavailableImage() { image.closest('.article-cover').hidden = true; }
      image.addEventListener('error', hideUnavailableImage);
      if (image.complete && !image.naturalWidth) hideUnavailableImage();
    });
    setupMoodSelection();
    setupMoodChart();
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
