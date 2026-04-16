/**
 * 格式化日期时间为 yyyy-MM-dd HH:mm:ss
 * @param {string|Date|number} date - 日期字符串、Date对象或时间戳
 * @returns {string} 格式化后的日期时间字符串
 */
export function formatDateTime(date) {
    if (!date) return '-'

    let d
    if (typeof date === 'string') {
        // 处理 ISO 格式和其他字符串格式
        d = new Date(date)
    } else if (typeof date === 'number') {
        // 处理时间戳
        d = new Date(date)
    } else if (date instanceof Date) {
        d = date
    } else {
        return '-'
    }

    // 检查日期是否有效
    if (isNaN(d.getTime())) return '-'

    const year = d.getFullYear()
    const month = String(d.getMonth() + 1).padStart(2, '0')
    const day = String(d.getDate()).padStart(2, '0')
    const hours = String(d.getHours()).padStart(2, '0')
    const minutes = String(d.getMinutes()).padStart(2, '0')
    const seconds = String(d.getSeconds()).padStart(2, '0')

    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
}

/**
 * 格式化日期为 yyyy-MM-dd
 * @param {string|Date|number} date - 日期字符串、Date对象或时间戳
 * @returns {string} 格式化后的日期字符串
 */
export function formatDate(date) {
    const dateTime = formatDateTime(date)
    return dateTime === '-' ? '-' : dateTime.split(' ')[0]
}

/**
 * 格式化时间为 HH:mm:ss
 * @param {string|Date|number} date - 日期字符串、Date对象或时间戳
 * @returns {string} 格式化后的时间字符串
 */
export function formatTime(date) {
    const dateTime = formatDateTime(date)
    return dateTime === '-' ? '-' : dateTime.split(' ')[1]
}
