// Purpose: Small vanilla-JavaScript enhancements for theme switching and password visibility controls.
(function () {
  var themeStorageKey = 'mindwell-theme';

  function getStoredTheme() {
    try {
      var stored = window.localStorage.getItem(themeStorageKey);
      return stored === 'dark' || stored === 'light' ? stored : null;
    } catch (error) {
      return null;
    }
  }

  function getPreferredTheme() {
    // Default to dark mode unless the user explicitly stores a light/dark preference.
    var stored = getStoredTheme();
    if (stored) return stored;

    return 'dark';
  }

  function updateThemeButtons(theme) {
    document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
      var isDark = theme === 'dark';
      button.setAttribute('aria-pressed', isDark ? 'true' : 'false');
      button.setAttribute('aria-label', isDark ? 'Switch to light mode' : 'Switch to dark mode');

      var label = button.querySelector('.sr-only');
      if (label) label.textContent = isDark ? 'Switch to light mode' : 'Switch to dark mode';
    });
  }

  function applyTheme(theme, persist) {
    var nextTheme = theme === 'dark' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-theme', nextTheme);

    if (persist) {
      try {
        window.localStorage.setItem(themeStorageKey, nextTheme);
      } catch (error) {
        // localStorage can be blocked in private browsing; the current page still updates.
      }
    }

    updateThemeButtons(nextTheme);
  }

  function setupThemeToggle() {
    // Synchronizes all header theme toggles because public, user, and admin masters share this script.
    applyTheme(getPreferredTheme(), false);

    document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
      button.addEventListener('click', function () {
        var current = document.documentElement.getAttribute('data-theme') || getPreferredTheme();
        applyTheme(current === 'dark' ? 'light' : 'dark', true);
      });
    });
  }

  function syncPasswordField(wrapper) {
    // Hides the reveal button for empty password fields and resets them to masked state.
    var input = wrapper.querySelector('input');
    var button = wrapper.querySelector('[data-password-toggle]');
    if (!input || !button) return;

    var hasValue = input.value.length > 0;
    wrapper.classList.toggle('has-value', hasValue);

    if (!hasValue) {
      input.type = 'password';
      button.classList.remove('is-visible');
      button.setAttribute('aria-pressed', 'false');
      button.setAttribute('aria-label', 'Show password');

      var emptyLabel = button.querySelector('.sr-only');
      if (emptyLabel) emptyLabel.textContent = 'Show password';
    }
  }

  function togglePassword(button) {
    // Toggles one password field without affecting other password inputs on the same page.
    var wrapper = button.closest('.password-field');
    if (!wrapper) return;

    var input = wrapper.querySelector('input');
    if (!input) return;

    var showing = input.type === 'text';
    input.type = showing ? 'password' : 'text';
    button.classList.toggle('is-visible', !showing);
    button.setAttribute('aria-pressed', showing ? 'false' : 'true');
    button.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');

    var label = button.querySelector('.sr-only');
    if (label) label.textContent = showing ? 'Show password' : 'Hide password';
  }

  function setupPasswordFields() {
    document.querySelectorAll('.password-field').forEach(function (wrapper) {
      var input = wrapper.querySelector('input');
      if (!input) return;

      syncPasswordField(wrapper);
      input.addEventListener('input', function () {
        syncPasswordField(wrapper);
      });
    });
  }

  function setupSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(function (link) {
      link.addEventListener('click', function (event) {
        var target = document.querySelector(link.getAttribute('href'));
        if (!target) return;

        event.preventDefault();
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
      });
    });
  }

  function setupCardReveal() {
    if (!('IntersectionObserver' in window)) return;

    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-revealed');
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12 });

    document.querySelectorAll('.card, .guided-activity-card, .quiz-review-card').forEach(function (card) {
      card.classList.add('reveal-pending');
      observer.observe(card);
    });
  }

  function setupQuizProgress() {
    var questions = document.querySelectorAll('.quiz-card');
    var progress = document.querySelector('.quiz-progress');
    if (!questions.length || !progress) return;

    function updateProgress() {
      var answered = document.querySelectorAll('.quiz-card input[type="radio"]:checked').length;
      progress.textContent = answered + ' of ' + questions.length + ' questions answered';
    }

    document.addEventListener('change', function (event) {
      if (event.target.matches('.quiz-card input[type="radio"]')) updateProgress();
    });
    updateProgress();
  }

  function setupDraftAutosave() {
    var draftFields = [
      { selector: 'textarea[name="response"]', key: 'activity-draft' },
      { selector: 'textarea[name="reflection"]', key: 'journal-draft' }
    ];

    draftFields.forEach(function (entry) {
      var field = document.querySelector(entry.selector);
      if (!field) return;

      try {
        var saved = window.localStorage.getItem(entry.key);
        if (saved && !field.value) field.value = saved;
      } catch (error) {
        return;
      }

      field.addEventListener('input', function () {
        try {
          window.localStorage.setItem(entry.key, field.value);
        } catch (error) {
          // Draft saving is progressive enhancement only.
        }
      });
    });
  }

  function setupConfirmActions() {
    document.addEventListener('click', function (event) {
      var action = event.target.closest('[data-confirm]');
      if (!action) return;

      var message = action.getAttribute('data-confirm');
      if (message && !window.confirm(message)) {
        event.preventDefault();
        event.stopPropagation();
      }
    });
  }

  function setupLessonInteractions() {
    document.addEventListener('click', function (event) {
      var revealButton = event.target.closest('[data-lesson-reveal]');
      if (revealButton) {
        var reveal = revealButton.closest('.lesson-reveal');
        if (!reveal) return;

        var shouldReveal = !reveal.classList.contains('is-revealed');
        reveal.classList.toggle('is-revealed', shouldReveal);
        revealButton.setAttribute('aria-expanded', shouldReveal ? 'true' : 'false');
        revealButton.textContent = shouldReveal ? 'Hide learning check' : 'Reveal learning check';
        return;
      }

      var flipCard = event.target.closest('[data-flip-card]');
      if (flipCard) {
        var flipped = !flipCard.classList.contains('is-flipped');
        flipCard.classList.toggle('is-flipped', flipped);
        flipCard.setAttribute('aria-pressed', flipped ? 'true' : 'false');
      }
    });
  }

  function setupVloToggles() {
    document.addEventListener('click', function (event) {
      var button = event.target.closest('[data-vlo-toggle]');
      if (!button) return;

      var panel = button.closest('[data-vlo-panel]') || button.closest('.module-learning-object');
      if (!panel) return;

      var targetId = button.getAttribute('data-vlo-target') || button.getAttribute('aria-controls');
      var target = targetId ? document.getElementById(targetId) : null;
      if (!target || !target.classList.contains('vlo-content')) return;

      var shouldOpen = target.hasAttribute('hidden');

      panel.querySelectorAll('.vlo-content').forEach(function (card) {
        card.setAttribute('hidden', 'hidden');
      });

      panel.querySelectorAll('[data-vlo-toggle]').forEach(function (toggle) {
        toggle.classList.remove('is-active');
        toggle.setAttribute('aria-expanded', 'false');
      });

      if (shouldOpen) {
        target.removeAttribute('hidden');
        button.classList.add('is-active');
        button.setAttribute('aria-expanded', 'true');
      }
    });
  }

  function generateTemporaryPassword() {
    var upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ';
    var lower = 'abcdefghijkmnopqrstuvwxyz';
    var digits = '23456789';
    var symbols = '!@#$%';
    var all = upper + lower + digits + symbols;

    function pick(source) {
      return source.charAt(Math.floor(Math.random() * source.length));
    }

    var chars = [pick(upper), pick(lower), pick(digits), pick(symbols)];
    while (chars.length < 14) chars.push(pick(all));

    for (var i = chars.length - 1; i > 0; i--) {
      var j = Math.floor(Math.random() * (i + 1));
      var temp = chars[i];
      chars[i] = chars[j];
      chars[j] = temp;
    }

    return chars.join('');
  }

  function setupPasswordGenerator() {
    document.addEventListener('click', function (event) {
      var button = event.target.closest('[data-password-generate]');
      if (!button) return;

      event.preventDefault();
      var row = button.closest('.password-reset-actions') || button.closest('.password-reset-panel') || document;
      var input = row.querySelector('input[name="tempPassword"]');
      if (!input) return;

      input.value = generateTemporaryPassword();
      input.dispatchEvent(new Event('input', { bubbles: true }));
      input.focus();
    });
  }

  function setupPasswordStrength() {
    function evaluatePassword(password) {
      return {
        length: password.length >= 8,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /[0-9]/.test(password),
        special: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)
      };
    }

    function updateStrength(scope, passwordInput) {
      var password = passwordInput.value || '';
      var criteria = evaluatePassword(password);
      var metCount = Object.keys(criteria).filter(function (key) { return criteria[key]; }).length;
      var fill = scope.querySelector('.strength-fill');
      var strengthText = scope.querySelector('.strength-text span');

      Object.keys(criteria).forEach(function (key) {
        var icon = scope.querySelector('.criteria-' + key);
        if (!icon) return;
        icon.textContent = criteria[key] ? 'OK' : 'No';
        icon.classList.toggle('met', criteria[key]);
      });

      if (!fill || !strengthText) return;

      var level = 'empty';
      var label = '';
      if (password.length > 0 && metCount <= 2) {
        level = 'weak';
        label = 'Weak';
      } else if (metCount === 3) {
        level = 'fair';
        label = 'Fair';
      } else if (metCount === 4) {
        level = 'good';
        label = 'Good';
      } else if (metCount === 5) {
        level = 'strong';
        label = 'Strong';
      }

      fill.className = 'strength-fill ' + level;
      strengthText.textContent = label;
    }

    function updateMatch(scope, passwordInput, confirmInput) {
      var indicator = scope.querySelector('.match-indicator');
      if (!indicator || !confirmInput) return;

      if (!confirmInput.value) {
        indicator.textContent = '';
        indicator.classList.remove('show', 'error');
      } else if (passwordInput.value === confirmInput.value) {
        indicator.textContent = 'Passwords match';
        indicator.classList.add('show');
        indicator.classList.remove('error');
      } else {
        indicator.textContent = 'Passwords do not match';
        indicator.classList.add('show', 'error');
      }
    }

    document.querySelectorAll('[data-password-strength]').forEach(function (passwordInput) {
      var scope = passwordInput.closest('[data-password-strength-scope]') || passwordInput.closest('.card') || document;
      var confirmInput = scope.querySelector('[data-password-confirm]');

      function updateAll() {
        updateStrength(scope, passwordInput);
        updateMatch(scope, passwordInput, confirmInput);
      }

      passwordInput.addEventListener('input', updateAll);
      if (confirmInput) confirmInput.addEventListener('input', updateAll);
      updateAll();
    });
  }

  function setupMoodCards() {
    document.querySelectorAll('.mood-option-card input[type="radio"]').forEach(function (input) {
      input.addEventListener('change', function () {
        document.querySelectorAll('.mood-option-card').forEach(function (card) {
          var cardInput = card.querySelector('input[type="radio"]');
          card.classList.toggle('is-selected', !!cardInput && cardInput.checked);
        });
      });

      if (input.checked) {
        var card = input.closest('.mood-option-card');
        if (card) card.classList.add('is-selected');
      }
    });
  }

  function setupRegisterGate() {
    var trigger = document.querySelector('[data-register-gate]');
    var modal = document.querySelector('[data-register-gate-modal]');
    var backdrop = document.querySelector('[data-register-gate-backdrop]');
    if (!trigger || !modal || !backdrop) return;

    var storageKey = 'mindwell-register-gate';
    var accepted = false;
    try {
      accepted = window.localStorage.getItem(storageKey) === 'accepted';
    } catch (error) {
      accepted = false;
    }

    function closeGate() {
      modal.setAttribute('hidden', 'hidden');
      backdrop.setAttribute('hidden', 'hidden');
      document.body.classList.remove('register-gate-open');
      try {
        window.localStorage.setItem(storageKey, 'accepted');
      } catch (error) {
        // Registration remains usable even if browser storage is blocked.
      }
    }

    if (!accepted) {
      modal.removeAttribute('hidden');
      backdrop.removeAttribute('hidden');
      document.body.classList.add('register-gate-open');
    }

    document.querySelectorAll('[data-register-gate-close]').forEach(function (button) {
      button.addEventListener('click', closeGate);
    });
  }

  function setupModuleAuthGate() {
    var backdrop = document.getElementById('moduleGateBackdrop');
    var modal = document.getElementById('moduleGateModal');
    if (!backdrop || !modal) return;

    var lastTrigger = null;

    function updateAuthReturnUrls(trigger) {
      var triggerHref = trigger ? trigger.getAttribute('href') || '' : '';
      var target = new URL(triggerHref, window.location.href);
      var returnUrl = target.pathname + target.search;
      var encodedReturnUrl = encodeURIComponent(returnUrl);
      modal.querySelectorAll('[data-login-return]').forEach(function (link) {
        var baseHref = (link.getAttribute('href') || '../Auth/Login.aspx').split('?')[0];
        link.setAttribute('href', baseHref + '?returnUrl=' + encodedReturnUrl);
      });
    }

    function openGate(trigger) {
      lastTrigger = trigger;
      updateAuthReturnUrls(trigger);
      backdrop.removeAttribute('hidden');
      modal.removeAttribute('hidden');
      document.body.classList.add('register-gate-open');
      var firstFocusable = modal.querySelector('a, button');
      if (firstFocusable) firstFocusable.focus();
    }

    function closeGate() {
      backdrop.setAttribute('hidden', 'hidden');
      modal.setAttribute('hidden', 'hidden');
      document.body.classList.remove('register-gate-open');
      if (lastTrigger) { lastTrigger.focus(); lastTrigger = null; }
    }

    document.querySelectorAll('[data-requires-auth="true"]').forEach(function (link) {
      link.addEventListener('click', function (e) {
        e.preventDefault();
        openGate(link);
      });
    });

    document.querySelectorAll('[data-module-gate-close]').forEach(function (btn) {
      btn.addEventListener('click', closeGate);
    });

    backdrop.addEventListener('click', closeGate);

    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && !modal.hasAttribute('hidden')) closeGate();
    });
  }

  function setupResetNotificationPopup() {
    document.querySelectorAll('[data-reset-notification-toggle]').forEach(function (button) {
      var panelId = button.getAttribute('aria-controls');
      var panel = panelId ? document.getElementById(panelId) : null;
      if (!panel || !panel.matches('[data-reset-notification-popover]')) return;

      function closePanel() {
        panel.setAttribute('hidden', 'hidden');
        button.setAttribute('aria-expanded', 'false');
      }

      button.addEventListener('click', function (event) {
        event.preventDefault();
        var shouldOpen = panel.hasAttribute('hidden');
        if (shouldOpen) panel.removeAttribute('hidden');
        else panel.setAttribute('hidden', 'hidden');
        button.setAttribute('aria-expanded', shouldOpen ? 'true' : 'false');
      });

      document.addEventListener('click', function (event) {
        if (panel.hasAttribute('hidden')) return;
        if (panel.contains(event.target) || button.contains(event.target)) return;
        closePanel();
      });

      document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') closePanel();
      });
    });
  }

  document.addEventListener('click', function (event) {
    var button = event.target.closest('[data-password-toggle]');
    if (!button) return;

    event.preventDefault();
    togglePassword(button);
  });

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      setupThemeToggle();
      setupPasswordFields();
      setupSmoothScroll();
      setupCardReveal();
      setupQuizProgress();
      setupDraftAutosave();
      setupConfirmActions();
      setupLessonInteractions();
      setupVloToggles();
      setupPasswordGenerator();
      setupPasswordStrength();
      setupMoodCards();
      setupRegisterGate();
      setupModuleAuthGate();
      setupResetNotificationPopup();
    });
  } else {
    setupThemeToggle();
    setupPasswordFields();
    setupSmoothScroll();
    setupCardReveal();
    setupQuizProgress();
    setupDraftAutosave();
    setupConfirmActions();
    setupLessonInteractions();
    setupVloToggles();
    setupPasswordGenerator();
    setupPasswordStrength();
    setupMoodCards();
    setupRegisterGate();
    setupModuleAuthGate();
    setupResetNotificationPopup();
  }
})();
