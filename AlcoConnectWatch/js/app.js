function app() {
  return {
    currentView: 'dashboard',
    sidebarOpen: true,
    mobileMenuOpen: false,
    showExportMenu: false,
    showUserMenu: false,

    user: JSON.parse(localStorage.getItem('acw_user') || '{"name":"Admin","email":"","role":"Administrator","initials":"AD"}'),

      sites: [],
      allSites: [],

    stats: {
      filesProcessed: 0,
      filesTarget: 0,
      activeSites: 0,
      activeSitesTarget: 0,
      complianceRate: 0,
      complianceTarget: 0,
      pendingAlerts: 0,
      pendingAlertsTarget: 0,
    },

    report: {
      date: '',
      site: '',
      generated: false,
      loading: false,
      activeTab: 'evac-only',
    },
    searchQuery: '',

    fileMonitor: {
      status: 'stopped',
      watchFolder: '',
      scanInterval: 5,
      lastScan: 'Never',
      filesDetectedToday: 0,
      errorsToday: 0,
      evacFilesToday: 0,
      alcoFilesToday: 0,
    },

    recentActivity: [],

    reportData: {
      evacOnly: [],
      alcoOnly: [],
      matched: [],
    },

    recentFiles: [],

    importLogs: [],

    settings: {
      watchFolder: '',
      scanInterval: 5,
      dbServer: '',
      dbName: '',
      sites: [],
      newSite: '',
      saved: false,
    },

    users: [],
    userModal: {
      open: false,
      editing: false,
      editId: null,
      form: { email: '', password: '', sites: [] },
    },

    logFilter: 'all',
    logSearch: '',

    get authHeaders() {
      var token = localStorage.getItem('acw_token');
      var headers = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = 'Bearer ' + token;
      return headers;
    },

    async apiFetch(url, options) {
      options = options || {};
      options.headers = this.authHeaders;
      var res = await fetch('api/' + url, options);
      if (!res.ok) throw new Error('API error: ' + res.status);
      return res.json();
    },

    async init() {
      if (!localStorage.getItem('acw_token')) {
        window.location.href = 'index.html';
        return;
      }

      document.addEventListener('click', function(e) {
        if (!e.target.closest('.export-dropdown')) this.showExportMenu = false;
        if (!e.target.closest('.user-menu-wrapper')) this.showUserMenu = false;
      }.bind(this));

      await this.loadSites();
      await this.loadDashboard();
    },

    async loadSites() {
      try {
        this.sites = await this.apiFetch('report/sites');
      } catch (e) {
        this.sites = ['Dalgaranga', 'Mt Magnet', 'Edna May'];
      }
      },
      async loadAllSites() {
          try {
              var data = await this.apiFetch('settings');
              this.allSites = data.sites || ['Dalgaranga', 'Mt Magnet', 'Edna May'];
          } catch (e) {
              this.allSites = ['Dalgaranga', 'Mt Magnet', 'Edna May'];
          }
      },

    async loadDashboard() {
      try {
        var results = await Promise.all([
          this.apiFetch('dashboard/stats'),
          this.apiFetch('dashboard/activity'),
        ]);
        var stats = results[0];
        var activity = results[1];
        this.stats.filesTarget = stats.filesProcessed;
        this.stats.activeSitesTarget = stats.activeSites;
        this.stats.complianceTarget = stats.complianceRate;
        this.stats.pendingAlertsTarget = stats.pendingAlerts;
        this.recentActivity = activity;
        this.animateCounters();
      } catch (e) {
        console.error('Failed to load dashboard:', e);
      }
    },

    async navigateTo(view) {
      this.currentView = view;
      this.mobileMenuOpen = false;
      if (view === 'dashboard') {
        await this.loadDashboard();
      } else if (view === 'file-monitor') {
        await this.loadFileMonitor();
      } else if (view === 'import-logs') {
        await this.loadImportLogs();
      } else if (view === 'users') {
        await this.loadUsers();
      } else if (view === 'settings') {
        await this.loadSettings();
      }
    },

    animateCounters() {
      this.stats.filesProcessed = 0;
      this.stats.activeSites = 0;
      this.stats.complianceRate = 0;
      this.stats.pendingAlerts = 0;

      var duration = 1500;
      var steps = 60;
      var interval = duration / steps;
      var step = 0;
      var self = this;

      var timer = setInterval(function() {
        step++;
        var progress = step / steps;
        var eased = 1 - Math.pow(1 - progress, 3);

        self.stats.filesProcessed = Math.round(eased * self.stats.filesTarget);
        self.stats.activeSites = Math.round(eased * self.stats.activeSitesTarget);
        self.stats.complianceRate = parseFloat((eased * self.stats.complianceTarget).toFixed(1));
        self.stats.pendingAlerts = Math.round(eased * self.stats.pendingAlertsTarget);

        if (step >= steps) clearInterval(timer);
      }, interval);
    },

    async generateReport() {
      if (!this.report.date || !this.report.site) return;
      this.report.loading = true;
      this.report.generated = false;

      try {
        var data = await this.apiFetch('report/generate', {
          method: 'POST',
          body: JSON.stringify({ date: this.report.date, site: this.report.site }),
        });
        this.reportData.evacOnly = data.evacOnly || [];
        this.reportData.alcoOnly = data.alcoOnly || [];
        this.reportData.matched = data.matched || [];
        this.report.generated = true;
        this.report.activeTab = 'evac-only';
      } catch (e) {
        alert('Failed to generate report: ' + e.message);
      } finally {
        this.report.loading = false;
      }
    },

    resetReport() {
      this.report.date = '';
      this.report.site = '';
      this.report.generated = false;
      this.report.loading = false;
      this.reportData = { evacOnly: [], alcoOnly: [], matched: [] };
    },

    getWizardStepState(step) {
      if (step === 1) return this.report.date ? 'completed' : 'active';
      if (step === 2) {
        if (this.report.site) return 'completed';
        if (this.report.date) return 'active';
        return '';
      }
      if (step === 3) {
        if (this.report.generated) return 'completed';
        if (this.report.date && this.report.site) return 'active';
        return '';
      }
      return '';
    },

    get filteredEvacOnly() {
      if (!this.searchQuery) return this.reportData.evacOnly;
      var q = this.searchQuery.toLowerCase();
      return this.reportData.evacOnly.filter(function(r) {
        return r.name.toLowerCase().includes(q) ||
          r.organisation.toLowerCase().includes(q) ||
          r.extractedId.includes(q);
      });
    },

    get filteredAlcoOnly() {
      if (!this.searchQuery) return this.reportData.alcoOnly;
      var q = this.searchQuery.toLowerCase();
      return this.reportData.alcoOnly.filter(function(r) {
        return r.staffName.toLowerCase().includes(q) || r.staffId.includes(q);
      });
    },

    get filteredMatched() {
      if (!this.searchQuery) return this.reportData.matched;
      var q = this.searchQuery.toLowerCase();
      return this.reportData.matched.filter(function(r) {
        return r.evacName.toLowerCase().includes(q) ||
          r.staffName.toLowerCase().includes(q) ||
          r.extractedId.includes(q);
      });
    },

    async exportReport(format) {
      this.showExportMenu = false;
      try {
        var url = format === 'xlsx' ? 'export/excel' : 'export/csv';
        var body = { date: this.report.date, site: this.report.site };
        if (format === 'csv') body.group = this.report.activeTab;

        var res = await fetch('api/' + url, {
          method: 'POST',
          headers: this.authHeaders,
          body: JSON.stringify(body),
        });
        if (!res.ok) throw new Error('Export failed');
        var blob = await res.blob();
        var a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        var ext = format === 'xlsx' ? 'xlsx' : 'csv';
        a.download = 'Compliance_Report_' + this.report.site.replace(/\s/g, '_') + '_' + this.report.date + '.' + ext;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(a.href);
      } catch (e) {
        alert('Export failed: ' + e.message);
      }
    },

    async loadFileMonitor() {
      try {
        var results = await Promise.all([
          this.apiFetch('filemonitor/status'),
          this.apiFetch('filemonitor/recent-files'),
        ]);
        var status = results[0];
        this.fileMonitor.status = status.status;
        this.fileMonitor.watchFolder = status.watchFolder;
        this.fileMonitor.scanInterval = status.scanInterval;
        this.fileMonitor.lastScan = status.lastScan;
        this.fileMonitor.filesDetectedToday = status.filesDetectedToday;
        this.fileMonitor.errorsToday = status.errorsToday;
        this.fileMonitor.evacFilesToday = status.evacFilesToday;
        this.fileMonitor.alcoFilesToday = status.alcoFilesToday;
        this.recentFiles = results[1];
      } catch (e) {
        console.error('Failed to load file monitor:', e);
      }
    },

    async restartWatcher() {
      try {
        await this.apiFetch('filemonitor/restart', { method: 'POST', body: '{}' });
        await this.loadFileMonitor();
      } catch (e) {
        alert('Failed to restart watcher: ' + e.message);
      }
    },

    async loadImportLogs() {
      try {
        this.importLogs = await this.apiFetch('importlogs');
      } catch (e) {
        console.error('Failed to load import logs:', e);
      }
    },

    get filteredLogs() {
      var logs = this.importLogs;
      if (this.logFilter !== 'all') {
        if (this.logFilter === 'success') logs = logs.filter(function(l) { return l.status === 'Success'; });
        if (this.logFilter === 'error') logs = logs.filter(function(l) { return l.status === 'Error'; });
        if (this.logFilter === 'evac') logs = logs.filter(function(l) { return l.fileType === 'Evac'; });
        if (this.logFilter === 'alco') logs = logs.filter(function(l) { return l.fileType === 'AlcoConnect'; });
      }
      if (this.logSearch) {
        var q = this.logSearch.toLowerCase();
        logs = logs.filter(function(l) { return l.fileName.toLowerCase().includes(q); });
      }
      return logs;
    },

    async loadSettings() {
      try {
        var data = await this.apiFetch('settings');
        this.settings.watchFolder = data.watchFolder;
        this.settings.scanInterval = data.scanInterval;
        this.settings.dbServer = data.dbServer;
        this.settings.dbName = data.dbName;
        this.settings.sites = data.sites || [];
      } catch (e) {
        console.error('Failed to load settings:', e);
      }
    },

    async saveSettings() {
      try {
        // Use POST /update endpoint to avoid 403 on servers that block PUT
        await this.apiFetch('settings/update', {
          method: 'POST',
          body: JSON.stringify({
            watchFolder: this.settings.watchFolder,
            scanInterval: parseInt(this.settings.scanInterval),
            sites: this.settings.sites,
          }),
        });
        this.settings.saved = true;
        var self = this;
        setTimeout(function() { self.settings.saved = false; }, 2500);
      } catch (e) {
        alert('Failed to save settings: ' + e.message);
      }
    },

    addSite() {
      var name = this.settings.newSite.trim();
      if (name && !this.settings.sites.includes(name)) {
        this.settings.sites.push(name);
        this.settings.newSite = '';
      }
    },

    removeSite(index) {
      this.settings.sites.splice(index, 1);
    },

    async loadUsers() {
      try {
          this.users = await this.apiFetch('users');
          await this.loadAllSites();  // ← YE ADD KARO
      } catch (e) {
        console.error('Failed to load users:', e);
      }
    },

    openUserModal() {
      this.userModal.editing = false;
      this.userModal.editId = null;
      this.userModal.form = { email: '', password: '', sites: [] };
      this.userModal.open = true;
    },

    editUser(id) {
      var u = this.users.find(function(u) { return u.id === id; });
      if (!u) return;
      this.userModal.editing = true;
      this.userModal.editId = id;
      this.userModal.form = {
        email: u.email,
        password: u.password,
        sites: u.sites.slice(),
      };
      this.userModal.open = true;
    },

    toggleUserSite(site) {
      var idx = this.userModal.form.sites.indexOf(site);
      if (idx === -1) this.userModal.form.sites.push(site);
      else this.userModal.form.sites.splice(idx, 1);
    },

    async saveUser() {
      if (!this.userModal.form.email || !this.userModal.form.password) return;
      try {
        if (this.userModal.editing) {
          // Use POST /update endpoint to avoid 403 on servers that block PUT
          await this.apiFetch('users/' + this.userModal.editId + '/update', {
            method: 'POST',
            body: JSON.stringify({
              email: this.userModal.form.email,
              password: this.userModal.form.password,
              sites: this.userModal.form.sites,
            }),
          });
        } else {
          await this.apiFetch('users', {
            method: 'POST',
            body: JSON.stringify({
              email: this.userModal.form.email,
              password: this.userModal.form.password,
              sites: this.userModal.form.sites,
            }),
          });
        }
        this.userModal.open = false;
        await this.loadUsers();
          await this.loadSites();
      } catch (e) {
        alert('Failed to save user: ' + e.message);
      }
    },

    async deleteUser(id) {
      if (!confirm('Are you sure you want to delete this user?')) return;
      try {
        // Use POST /delete endpoint to avoid 403 on servers that block DELETE
        await this.apiFetch('users/' + id + '/delete', { method: 'POST', body: '{}' });
        await this.loadUsers();
      } catch (e) {
        alert('Failed to delete user: ' + e.message);
      }
    },

    formatDate(dateStr) {
      if (!dateStr) return '';
      var d = new Date(dateStr);
      return d.toLocaleDateString('en-AU', { day: '2-digit', month: 'short', year: 'numeric' });
    },

    logout() {
      localStorage.removeItem('acw_token');
      localStorage.removeItem('acw_user');
      window.location.href = 'index.html';
    },
  };
}
