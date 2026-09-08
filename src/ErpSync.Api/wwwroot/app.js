// 前端只負責呼叫 spec.md §10 的查詢 API 並把結果畫成表格，沒有任何業務邏輯（spec.md §11）
const api = {
  purchaseOrders: (supplier, companyCode) => {
    const params = new URLSearchParams();
    if (supplier) params.set('supplier', supplier);
    if (companyCode) params.set('companyCode', companyCode);
    return getJson(`/api/purchase-orders?${params}`);
  },
  items: (id) => getJson(`/api/purchase-orders/${encodeURIComponent(id)}/items`),
  anomalies: (ruleType) => {
    const params = new URLSearchParams();
    if (ruleType) params.set('ruleType', ruleType);
    return getJson(`/api/anomalies?${params}`);
  },
  syncLogs: () => getJson('/api/sync-logs'),
  runSync: () => postJson('/api/sync/run'),
};

async function getJson(url) {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json();
}

async function postJson(url) {
  const res = await fetch(url, { method: 'POST' });
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json();
}

const statusEl = document.getElementById('status');
function setStatus(text) {
  statusEl.textContent = text;
}

// 後端回傳的時間是 UTC（DateTime.UtcNow），JSON 沒帶時區，這裡補 Z 再轉成本地時間顯示
function formatUtc(value) {
  if (!value) return '';
  const iso = value.endsWith('Z') ? value : `${value}Z`;
  return new Date(iso).toLocaleString('zh-TW', { hour12: false });
}

function renderRows(tbody, rows, columnCount, rowRenderer) {
  tbody.replaceChildren();
  if (rows.length === 0) {
    const tr = document.createElement('tr');
    const td = document.createElement('td');
    td.colSpan = columnCount;
    td.className = 'empty';
    td.textContent = '沒有資料';
    tr.append(td);
    tbody.append(tr);
    return;
  }
  rows.forEach((row) => tbody.append(rowRenderer(row)));
}

function cells(...values) {
  return values.map((value) => {
    const td = document.createElement('td');
    td.textContent = value ?? '';
    return td;
  });
}

// ---- 採購單 ----
const ordersBody = document.querySelector('#orders-table tbody');

async function loadOrders() {
  const supplier = document.getElementById('filter-supplier').value.trim();
  const companyCode = document.getElementById('filter-company').value.trim();
  const orders = await api.purchaseOrders(supplier, companyCode);

  renderRows(ordersBody, orders, 9, (order) => {
    const tr = document.createElement('tr');
    tr.className = 'order-row';
    tr.append(...cells(
      order.purchaseOrderNumber,
      order.purchaseOrderType,
      order.companyCode,
      order.purchasingOrganization,
      order.supplier,
      order.purchaseOrderDate,
      order.documentCurrency,
      order.purchasingCompletenessStatus ? '是' : '否',
      formatUtc(order.lastSyncedAt),
    ));
    tr.addEventListener('click', () => toggleItems(tr, order.purchaseOrderNumber));
    return tr;
  });
  setStatus(`採購單 ${orders.length} 筆`);
}

async function toggleItems(orderRow, purchaseOrderNumber) {
  const next = orderRow.nextElementSibling;
  if (next?.classList.contains('items-row')) {
    next.remove();
    return;
  }

  const items = await api.items(purchaseOrderNumber);
  const tr = document.createElement('tr');
  tr.className = 'items-row';
  const td = document.createElement('td');
  td.colSpan = 9;
  td.append(buildItemsTable(items));
  tr.append(td);
  orderRow.after(tr);
}

function buildItemsTable(items) {
  const table = document.createElement('table');
  const thead = document.createElement('thead');
  const headRow = document.createElement('tr');
  ['品項', '物料', '工廠', '數量', '單位', '單價', '金額', '交期'].forEach((label) => {
    const th = document.createElement('th');
    th.textContent = label;
    headRow.append(th);
  });
  thead.append(headRow);

  const tbody = document.createElement('tbody');
  renderRows(tbody, items, 8, (item) => {
    const tr = document.createElement('tr');
    tr.append(...cells(
      item.purchaseOrderItemNumber,
      item.material,
      item.plant,
      item.orderQuantity,
      item.purchaseOrderQuantityUnit,
      item.netPriceAmount,
      item.netAmount,
      item.deliveryDate,
    ));
    return tr;
  });

  table.append(thead, tbody);
  return table;
}

// ---- 異常清單 ----
const anomaliesBody = document.querySelector('#anomalies-table tbody');

async function loadAnomalies() {
  const ruleType = document.getElementById('filter-rule').value;
  const anomalies = await api.anomalies(ruleType);

  renderRows(anomaliesBody, anomalies, 7, (anomaly) => {
    const tr = document.createElement('tr');
    tr.append(...cells(
      anomaly.id,
      anomaly.ruleType,
      anomaly.purchaseOrder,
      anomaly.purchaseOrderItemNumber,
      anomaly.detail,
      formatUtc(anomaly.detectedAt),
      anomaly.notifiedAt ? formatUtc(anomaly.notifiedAt) : '未通知',
    ));
    return tr;
  });
  setStatus(`異常 ${anomalies.length} 筆`);
}

// ---- 同步紀錄 ----
const logsBody = document.querySelector('#logs-table tbody');

async function loadLogs() {
  const logs = await api.syncLogs();
  renderRows(logsBody, logs, 5, (log) => {
    const tr = document.createElement('tr');
    tr.append(...cells(log.id, formatUtc(log.runAt), log.recordsFetched, log.recordsNew, log.anomaliesFound));
    return tr;
  });
  setStatus(`同步紀錄 ${logs.length} 筆`);
}

// ---- 分頁切換 ----
const loaders = { orders: loadOrders, anomalies: loadAnomalies, logs: loadLogs };
let currentTab = 'orders';

async function reload() {
  try {
    setStatus('載入中…');
    await loaders[currentTab]();
  } catch (err) {
    setStatus(`載入失敗：${err.message}`);
  }
}

document.querySelectorAll('.tab').forEach((tab) => {
  tab.addEventListener('click', () => {
    currentTab = tab.dataset.tab;
    document.querySelectorAll('.tab').forEach((t) => t.classList.toggle('active', t === tab));
    document.querySelectorAll('.panel').forEach((panel) => {
      panel.classList.toggle('hidden', panel.id !== `tab-${currentTab}`);
    });
    reload();
  });
});

document.getElementById('orders-filter-btn').addEventListener('click', reload);
document.getElementById('filter-rule').addEventListener('change', reload);

document.getElementById('sync-btn').addEventListener('click', async (e) => {
  e.target.disabled = true;
  try {
    setStatus('同步中…');
    const result = await api.runSync();
    // 先重載清單再設狀態列，否則會被 loader 自己的「N 筆」訊息蓋掉
    await loaders[currentTab]();
    setStatus(`同步完成：抓取 ${result.recordsFetched} 筆、新增 ${result.recordsNew} 筆、異常 ${result.anomaliesFound} 筆`);
  } catch (err) {
    setStatus(`同步失敗：${err.message}`);
  } finally {
    e.target.disabled = false;
  }
});

reload();
