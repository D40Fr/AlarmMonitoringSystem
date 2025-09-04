// AlarmMonitoringSystem.Web/wwwroot/js/signalr-client.js

// Enhanced SignalR client for real-time updates without page refresh
class AlarmMonitoringSignalR {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.currentPage = this.getCurrentPage();
        this.updateInterval = null;
    }

    // Initialize and start connection
    async init() {
        try {
            // Create connection with better configuration
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/alarmHub")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Warning) // Reduce noise
                .build();

            // Set up event handlers
            this.setupEventHandlers();

            // Start connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;

            console.log("✅ SignalR connected successfully");
            this.updateConnectionStatus("Connected", "success");

            // Join groups based on current page
            await this.joinRelevantGroups();

            // Start periodic health check
            this.startHealthCheck();

        } catch (error) {
            console.error("❌ SignalR connection failed:", error);
            this.updateConnectionStatus("Connection failed", "error");
            this.scheduleReconnect();
        }
    }

    // Get current page type
    getCurrentPage() {
        const path = window.location.pathname.toLowerCase();
        if (path.includes('/home') || path === '/') return 'dashboard';
        if (path.includes('/alarms')) return 'alarms';
        if (path.includes('/clients')) return 'clients';
        return 'other';
    }

    // Set up all SignalR event handlers
    setupEventHandlers() {
        // Connection events
        this.connection.onreconnecting(() => {
            this.isConnected = false;
            console.log("🔄 SignalR reconnecting...");
            this.updateConnectionStatus("Reconnecting...", "warning");
        });

        this.connection.onreconnected(() => {
            this.isConnected = true;
            console.log("✅ SignalR reconnected");
            this.updateConnectionStatus("Connected", "success");
            this.joinRelevantGroups();
        });

        this.connection.onclose(() => {
            this.isConnected = false;
            console.log("🔌 SignalR connection closed");
            this.updateConnectionStatus("Disconnected", "error");
            this.scheduleReconnect();
        });

        // Alarm events
        this.connection.on("NewAlarm", (alarm) => {
            console.log("🚨 New alarm received:", alarm);
            this.handleNewAlarm(alarm);
        });

        this.connection.on("AlarmAcknowledged", (data) => {
            console.log("✅ Alarm acknowledged:", data);
            this.handleAlarmAcknowledged(data);
        });

        // Client events
        this.connection.on("ClientConnected", (client) => {
            console.log("🔗 Client connected:", client);
            this.handleClientConnected(client);
        });

        this.connection.on("ClientDisconnected", (data) => {
            console.log("🔌 Client disconnected:", data);
            this.handleClientDisconnected(data);
        });

        this.connection.on("ClientStatusChanged", (client) => {
            console.log("📊 Client status changed:", client);
            this.handleClientStatusChanged(client);
        });

        // Refresh events (for simple updates)
        this.connection.on("RefreshStats", () => {
            console.log("📊 Refreshing dashboard stats");
            this.refreshDashboardStats();
        });

        this.connection.on("RefreshClientList", () => {
            console.log("🔄 Refreshing client list");
            if (this.currentPage === 'clients') {
                this.refreshPage();
            }
        });

        this.connection.on("RefreshAlarmList", () => {
            console.log("🔄 Refreshing alarm list");
            if (this.currentPage === 'alarms') {
                this.refreshAlarmList();
            }
        });
    }

    // Join groups based on current page
    async joinRelevantGroups() {
        if (!this.isConnected) return;

        try {
            // Always join dashboard for basic stats
            await this.connection.invoke("JoinDashboard");

            // Join specific groups based on page
            if (this.currentPage === 'alarms') {
                await this.connection.invoke("JoinAlarms");
            } else if (this.currentPage === 'clients') {
                await this.connection.invoke("JoinClients");
            }

            console.log("📢 Joined SignalR groups for:", this.currentPage);
        } catch (error) {
            console.error("Error joining SignalR groups:", error);
        }
    }

    // Handle new alarm
    handleNewAlarm(alarm) {
        if (this.currentPage === 'dashboard') {
            this.updateDashboardStats();
            this.addNewAlarmToDashboard(alarm);
        } else if (this.currentPage === 'alarms') {
            this.addNewAlarmToList(alarm);
        }

        this.updateAlarmCounts();
    }

    // Handle alarm acknowledged
    handleAlarmAcknowledged(data) {
        if (this.currentPage === 'dashboard') {
            this.updateDashboardStats();
        } else if (this.currentPage === 'alarms') {
            this.updateAlarmInList(data.AlarmId, 'acknowledged');
        }

        this.updateAlarmCounts();
    }

    // Handle client connected
    handleClientConnected(client) {
        if (this.currentPage === 'dashboard') {
            this.updateConnectionCounts();
            this.updateClientInDashboard(client, 'connected');
        } else if (this.currentPage === 'clients') {
            this.updateClientInList(client, 'connected');
        }
    }

    // Handle client disconnected
    handleClientDisconnected(data) {
        if (this.currentPage === 'dashboard') {
            this.updateConnectionCounts();
            this.updateClientInDashboard({ clientId: data.ClientId }, 'disconnected');
        } else if (this.currentPage === 'clients') {
            this.updateClientInList({ clientId: data.ClientId }, 'disconnected');
        }
    }

    // Handle client status changed
    handleClientStatusChanged(client) {
        this.handleClientConnected(client); // Reuse the same logic
    }

    // Update dashboard statistics via AJAX
    async updateDashboardStats() {
        if (this.currentPage !== 'dashboard') return;

        try {
            // Update alarm stats
            const alarmResponse = await fetch('/Alarms/GetAlarmStats');
            if (alarmResponse.ok) {
                const alarmData = await alarmResponse.json();
                if (alarmData.success) {
                    this.updateStatCard('TotalAlarms', alarmData.stats.total);
                    this.updateStatCard('ActiveAlarms', alarmData.stats.active);
                    this.updateStatCard('UnacknowledgedAlarms', alarmData.stats.unacknowledged);
                }
            }

            // Update client stats
            const clientResponse = await fetch('/api/clients/status');
            if (clientResponse.ok) {
                const clientData = await clientResponse.json();
                if (clientData.success) {
                    this.updateStatCard('ConnectedClients', clientData.data.ConnectedToTcpServer);
                    this.updateStatCard('TotalClients', clientData.data.TotalRegistered);
                }
            }
        } catch (error) {
            console.error('Error updating dashboard stats:', error);
        }
    }

    // Update stat card values
    updateStatCard(statName, newValue) {
        // Look for elements that might contain the stat
        const selectors = [
            `[data-stat="${statName}"]`,
            `#${statName}`,
            `.${statName.toLowerCase()}`
        ];

        for (const selector of selectors) {
            const elements = document.querySelectorAll(selector);
            elements.forEach(el => {
                if (el.textContent.trim() !== newValue.toString()) {
                    el.textContent = newValue;
                    el.classList.add('realtime-update');
                    setTimeout(() => el.classList.remove('realtime-update'), 2000);
                }
            });
        }

        // Also try to find by text pattern in common dashboard elements
        this.updateStatByPattern(statName, newValue);
    }

    // Update stats by finding text patterns
    updateStatByPattern(statName, newValue) {
        const patterns = {
            'ConnectedClients': /Connected.*Clients.*:\s*\d+/i,
            'TotalClients': /Total.*Clients.*:\s*\d+/i,
            'ActiveAlarms': /Active.*Alarms.*:\s*\d+/i,
            'UnacknowledgedAlarms': /Unacknowledged.*:\s*\d+/i
        };

        if (patterns[statName]) {
            const elements = document.querySelectorAll('.card-body, .navbar-text, .h5, .h4, .h3');
            elements.forEach(el => {
                if (patterns[statName].test(el.textContent)) {
                    const oldText = el.textContent;
                    const newText = oldText.replace(/\d+/, newValue);
                    if (oldText !== newText) {
                        el.textContent = newText;
                        el.classList.add('realtime-update');
                        setTimeout(() => el.classList.remove('realtime-update'), 2000);
                    }
                }
            });
        }
    }

    // Add new alarm to dashboard recent alarms
    addNewAlarmToDashboard(alarm) {
        const recentAlarmsTable = document.querySelector('.card-header:contains("Recent Alarms") + .card-body table tbody');
        if (recentAlarmsTable) {
            // Remove oldest if we have too many
            const rows = recentAlarmsTable.querySelectorAll('tr');
            if (rows.length >= 10) {
                rows[rows.length - 1].remove();
            }

            // Add new alarm at top
            const newRow = this.createAlarmRow(alarm);
            recentAlarmsTable.insertAdjacentHTML('afterbegin', newRow);
        }
    }

    // Add new alarm to alarms list page
    addNewAlarmToList(alarm) {
        const alarmTable = document.querySelector('.table tbody');
        if (alarmTable) {
            const newRow = this.createAlarmTableRow(alarm);
            alarmTable.insertAdjacentHTML('afterbegin', newRow);
        }
    }

    // Update alarm in list when acknowledged
    updateAlarmInList(alarmId, action) {
        const alarmRow = document.querySelector(`[data-alarm-id="${alarmId}"]`);
        if (alarmRow && action === 'acknowledged') {
            const statusCell = alarmRow.querySelector('.alarm-status');
            if (statusCell) {
                statusCell.innerHTML = '<span class="badge badge-success"><i class="fas fa-check"></i> Acknowledged</span>';
                alarmRow.classList.add('realtime-update');
                setTimeout(() => alarmRow.classList.remove('realtime-update'), 2000);
            }
        }
    }

    // Create alarm row HTML for dashboard
    createAlarmRow(alarm) {
        const time = new Date(alarm.alarmTime).toLocaleTimeString();
        const severityBadge = this.getSeverityBadgeClass(alarm.severity);

        return `
            <tr data-alarm-id="${alarm.id}" class="realtime-update">
                <td>${time}</td>
                <td>${alarm.clientName}</td>
                <td><a href="/Alarms/Details/${alarm.id}">${alarm.title}</a></td>
                <td><span class="badge ${severityBadge}">${this.getSeverityText(alarm.severity)}</span></td>
                <td><span class="badge badge-warning">Pending</span></td>
            </tr>
        `;
    }

    // Create full alarm table row for alarms page
    createAlarmTableRow(alarm) {
        const time = new Date(alarm.alarmTime).toLocaleString();
        const severityBadge = this.getSeverityBadgeClass(alarm.severity);

        return `
            <tr data-alarm-id="${alarm.id}" class="realtime-update">
                <td><input type="checkbox" class="alarm-checkbox" value="${alarm.id}"></td>
                <td>${time}</td>
                <td><a href="/Clients/Details/${alarm.clientId}">${alarm.clientName}</a></td>
                <td><code>${alarm.alarmId}</code></td>
                <td><strong>${alarm.title}</strong></td>
                <td><span class="badge badge-secondary">${alarm.typeDisplay}</span></td>
                <td><span class="badge ${severityBadge}">${this.getSeverityText(alarm.severity)}</span></td>
                <td>${alarm.formattedValue || '-'}</td>
                <td><span class="badge badge-warning">Pending</span></td>
                <td>
                    <div class="btn-group">
                        <a href="/Alarms/Details/${alarm.id}" class="btn btn-info btn-sm"><i class="fas fa-eye"></i></a>
                        <form action="/Alarms/Acknowledge/${alarm.id}" method="post" style="display: inline;">
                            <button type="submit" class="btn btn-success btn-sm"><i class="fas fa-check"></i></button>
                        </form>
                    </div>
                </td>
            </tr>
        `;
    }

    // Helper methods for badge classes
    getSeverityBadgeClass(severity) {
        switch (severity) {
            case 4: return 'badge-dark'; // Critical
            case 3: return 'badge-danger'; // High
            case 2: return 'badge-warning'; // Medium
            case 1: return 'badge-info'; // Low
            default: return 'badge-secondary';
        }
    }

    getSeverityText(severity) {
        switch (severity) {
            case 4: return 'Critical';
            case 3: return 'High';
            case 2: return 'Medium';
            case 1: return 'Low';
            default: return 'Unknown';
        }
    }

    // Update alarm counts in navigation or other places
    updateAlarmCounts() {
        // This will trigger the dashboard stats update
        setTimeout(() => this.updateDashboardStats(), 500);
    }

    // Update connection counts
    updateConnectionCounts() {
        // This will trigger the dashboard stats update  
        setTimeout(() => this.updateDashboardStats(), 500);
    }

    // Update client in dashboard
    updateClientInDashboard(client, status) {
        // Simple approach - refresh the client status section
        this.updateConnectionCounts();
    }

    // Update client in list
    updateClientInList(client, status) {
        // For now, just refresh the page for client list updates
        // This can be enhanced later if needed
        setTimeout(() => this.refreshPage(), 1000);
    }

    // Refresh alarm list for alarms page
    refreshAlarmList() {
        if (this.currentPage === 'alarms') {
            // Refresh just the table content
            setTimeout(() => window.location.reload(), 1000);
        }
    }

    // Refresh dashboard stats (already implemented above)
    refreshDashboardStats() {
        this.updateDashboardStats();
    }

    // Simple page refresh (fallback)
    refreshPage() {
        setTimeout(() => window.location.reload(), 1000);
    }

    // Update connection status indicator
    updateConnectionStatus(message, type) {
        // Update navbar indicator
        const indicator = document.getElementById('signalr-indicator');
        if (indicator) {
            if (type === 'success') {
                indicator.className = 'fas fa-circle text-success';
                indicator.title = 'SignalR: Connected';
            } else if (type === 'warning') {
                indicator.className = 'fas fa-circle text-warning';
                indicator.title = 'SignalR: Reconnecting...';
            } else {
                indicator.className = 'fas fa-circle text-danger';
                indicator.title = 'SignalR: Disconnected';
            }
        }

        // Console status (less verbose)
        if (type !== 'success') {
            console.log(`SignalR: ${message}`);
        }
    }

    // Start health check
    startHealthCheck() {
        // Check connection health every 30 seconds
        this.updateInterval = setInterval(() => {
            if (this.isConnected && this.connection.state === signalR.HubConnectionState.Connected) {
                // Connection is healthy
                this.updateConnectionStatus("Connected", "success");
            } else if (this.connection.state === signalR.HubConnectionState.Reconnecting) {
                this.updateConnectionStatus("Reconnecting...", "warning");
            } else {
                this.updateConnectionStatus("Disconnected", "error");
            }
        }, 30000);
    }

    // Schedule reconnection
    scheduleReconnect() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);

            console.log(`🔄 Scheduling reconnect attempt ${this.reconnectAttempts} in ${delay}ms`);

            setTimeout(() => {
                this.init();
            }, delay);
        } else {
            console.log("❌ Max reconnection attempts reached");
            this.updateConnectionStatus("Connection lost", "error");
        }
    }

    // Get connection state
    getConnectionState() {
        return {
            isConnected: this.isConnected,
            connectionState: this.connection ? this.connection.state : 'Disconnected',
            reconnectAttempts: this.reconnectAttempts
        };
    }

    // Cleanup
    dispose() {
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
        }
        if (this.connection) {
            this.connection.stop();
        }
    }
}

// Global instance
let alarmSignalR = null;

// Initialize when page loads
document.addEventListener('DOMContentLoaded', function () {
    console.log("🚀 Initializing enhanced SignalR...");
    alarmSignalR = new AlarmMonitoringSignalR();
    alarmSignalR.init();
});

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    if (alarmSignalR) {
        alarmSignalR.dispose();
    }
});

// CSS for realtime updates (enhanced)
const style = document.createElement('style');
style.textContent = `
    .realtime-update {
        animation: fadeInOut 2s ease-in-out;
        transition: all 0.3s ease;
    }

    @keyframes fadeInOut {
        0% { background-color: transparent; }
        50% { background-color: rgba(40, 167, 69, 0.15); }
        100% { background-color: transparent; }
    }

    /* SignalR Status Indicators */
    #signalr-indicator {
        transition: color 0.3s ease;
    }

    /* Better visual feedback */
    .table tbody tr.realtime-update {
        border-left: 3px solid #28a745;
    }
`;
document.head.appendChild(style);